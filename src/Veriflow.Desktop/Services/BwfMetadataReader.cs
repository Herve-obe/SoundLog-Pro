using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Services
{
    public class BwfMetadataReader
    {
        public AudioMetadata ReadMetadataFromStream(string filePath)
        {
            var metadata = new AudioMetadata
            {
                Filename = System.IO.Path.GetFileName(filePath)
            };

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    // RIFF Header
                    if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "RIFF") return metadata;
                    br.ReadInt32(); // File Size
                    if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "WAVE") return metadata;

                    int sampleRate = 0;
                    int bytesPerSec = 0;
                    int channels = 0;
                    int bitsPerSample = 0;
                    long dataSize = 0;

                    // Chunk Loop
                    while (fs.Position < fs.Length - 8) // Ensure header exists
                    {
                        var chunkIdBytes = br.ReadBytes(4);
                        if (chunkIdBytes.Length < 4) break;
                        string chunkId = Encoding.ASCII.GetString(chunkIdBytes);
                        
                        int chunkSize = br.ReadInt32();
                        if (chunkSize < 0) break; // Invalid

                        long chunkStart = fs.Position;

                        // Parse Chunks
                        if (chunkId.Trim().ToLower() == "fmt") // "fmt "
                        {
                            // Basic WAV format
                            short audioFormat = br.ReadInt16(); // 1 = PCM, 3 = Float, 65534 = Ext
                            channels = br.ReadInt16();
                            sampleRate = br.ReadInt32();
                            bytesPerSec = br.ReadInt32();
                            short blockAlign = br.ReadInt16();
                            bitsPerSample = br.ReadInt16();
                            
                            metadata.Format = $"{sampleRate}Hz / {bitsPerSample}bit";
                            metadata.ChannelCount = channels;
                        }
                        else if (chunkId.Trim().ToLower() == "data")
                        {
                            dataSize = chunkSize;
                            // Estimate duration if we have format
                            if (bytesPerSec > 0)
                            {
                                double seconds = (double)dataSize / bytesPerSec;
                                metadata.Duration = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
                            }
                        }
                        else if (chunkId.ToLower() == "bext")
                        {
                            var data = br.ReadBytes(chunkSize);
                            ParseBextBytes(data, metadata, sampleRate);
                        }
                        else if (chunkId.ToLower() == "ixml")
                        {
                            var data = br.ReadBytes(chunkSize);
                            ParseIXmlBytes(data, metadata);
                        }
                        else
                        {
                            // Skip unknown
                            // fs.Seek(chunkSize, SeekOrigin.Current) might be unsafe if buffer read already advances? 
                            // No, ReadBytes advances.
                            // But we only ReadBytes for bext/ixml.
                            // For others, we assume we haven't read content yet.
                        }

                        // Align to 2 bytes
                        if (chunkSize % 2 != 0) chunkSize++;

                        // Seek to next chunk exactly
                        fs.Position = chunkStart + chunkSize;
                    }
                }
            }
            catch { }
            return metadata;
        }

        private void ParseBextBytes(byte[] data, AudioMetadata metadata, int sampleRate)
        {
            if (data.Length < 256 + 32 + 32 + 10 + 8 + 8) return;

            metadata.Scene = GetString(data, 0, 256).Trim(); 
            metadata.Originator = GetString(data, 256, 32).Trim();
            metadata.CreationDate = GetString(data, 256 + 32 + 32, 10) + " " + GetString(data, 256 + 32 + 32 + 10, 8);

            // Timecode
            long low = BitConverter.ToUInt32(data, 256 + 32 + 32 + 10 + 8);
            long high = BitConverter.ToUInt32(data, 256 + 32 + 32 + 10 + 8 + 4);
            long samplesSinceMidnight = low + (high << 32);

            metadata.TimecodeStart = SamplesToTimecode(samplesSinceMidnight, sampleRate);
            if (sampleRate > 0)
                metadata.TimeReferenceSeconds = (double)samplesSinceMidnight / sampleRate;
        }

        private void ParseIXmlBytes(byte[] data, AudioMetadata metadata)
        {
            try
            {
                string xmlString = Encoding.UTF8.GetString(data).Trim('\0');
                var doc = new XmlDocument();
                doc.LoadXml(xmlString);

                var projectNode = doc.SelectSingleNode("//PROJECT");
                var tapeNode = doc.SelectSingleNode("//TAPE");
                var sceneNode = doc.SelectSingleNode("//SCENE");
                var takeNode = doc.SelectSingleNode("//TAKE");
                
                if (sceneNode != null) metadata.Scene = sceneNode.InnerText;
                if (takeNode != null) metadata.Take = takeNode.InnerText;
                if (tapeNode != null) metadata.Tape = tapeNode.InnerText;

                var trackNodes = doc.SelectNodes("//TRACK_LIST/TRACK");
                if (trackNodes != null)
                {
                    metadata.TrackNames.Clear();
                    var tracks = new SortedDictionary<int, string>();
                    
                    foreach (XmlNode node in trackNodes)
                    {
                         var indexNode = node.SelectSingleNode("CHANNEL_INDEX");
                         var nameNode = node.SelectSingleNode("NAME");
                         
                         if (indexNode != null && nameNode != null && int.TryParse(indexNode.InnerText, out int idx))
                         {
                             // iXML is 1-based usually
                             tracks[idx] = nameNode.InnerText;
                         }
                    }

                    foreach(var kvp in tracks)
                    {
                        metadata.TrackNames.Add(kvp.Value);
                    }
                }
            }
            catch { }
        }

        private string GetString(byte[] buffer, int offset, int length)
        {
            return Encoding.ASCII.GetString(buffer, offset, length).Trim('\0');
        }

        private string SamplesToTimecode(long samples, int sampleRate)
        {
            if (sampleRate == 0) return "00:00:00:00";
            
            double totalSeconds = (double)samples / sampleRate;
            TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
            return t.ToString(@"hh\:mm\:ss");
        }
    }
}
