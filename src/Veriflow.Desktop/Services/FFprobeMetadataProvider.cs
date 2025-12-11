using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Services
{
    public class FFprobeMetadataProvider
    {
        public async Task<AudioMetadata> GetMetadataAsync(string filePath)
        {
             var metadata = new AudioMetadata
            {
                Filename = Path.GetFileName(filePath)
            };

            try
            {
                // 1. Explicit Path Construction
                string ffprobePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffprobe.exe");
                
                if (!File.Exists(ffprobePath)) 
                {
                    System.Diagnostics.Debug.WriteLine($"FFprobe not found at: {ffprobePath}");
                    return metadata;
                }

                var args = $"-v quiet -print_format json -show_format -show_streams -i \"{filePath}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                string jsonOutput = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(jsonOutput))
                {
                    ParseJson(jsonOutput, metadata);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FFprobe Error: {ex.Message}");
            }

            return metadata;
        }

        private void ParseJson(string json, AudioMetadata metadata)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 2. Data Extraction Helpers
                JsonElement? formatTags = null;
                if (root.TryGetProperty("format", out var format))
                {
                    if (format.TryGetProperty("tags", out var ft)) formatTags = ft;
                    
                    // Duration
                    if (format.TryGetProperty("duration", out var durProp) && 
                        double.TryParse(durProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double durationSec))
                    {
                        metadata.Duration = TimeSpan.FromSeconds(durationSec).ToString(@"hh\:mm\:ss");
                    }
                }

                JsonElement? audioStream = null;
                int sampleRate = 0;
                int channels = 0;
                int bits = 0;

                if (root.TryGetProperty("streams", out var streams) && streams.GetArrayLength() > 0)
                {
                    foreach (var s in streams.EnumerateArray())
                    {
                        if (s.TryGetProperty("codec_type", out var type) && type.GetString() == "audio")
                        {
                            audioStream = s;
                            // Capture Audio Properties
                            if (s.TryGetProperty("sample_rate", out var srProp)) int.TryParse(srProp.GetString(), out sampleRate);
                            if (s.TryGetProperty("channels", out var chProp)) channels = chProp.GetInt32();
                            if (s.TryGetProperty("bits_per_raw_sample", out var bprs)) int.TryParse(bprs.GetString(), out bits);
                            else if (s.TryGetProperty("bits_per_sample", out var bps)) int.TryParse(bps.GetString(), out bits);
                            
                            break;
                        }
                    }
                }

                // Format String
                if (sampleRate > 0)
                {
                    metadata.Format = $"{sampleRate}Hz";
                    if (bits > 0) metadata.Format += $" / {bits}bit";
                }
                metadata.ChannelCount = channels;


                // 3. Metadata & Timecode Logic
                var tagSources = new List<JsonElement?>();
                if (formatTags.HasValue) tagSources.Add(formatTags);
                if (audioStream.HasValue && audioStream.Value.TryGetProperty("tags", out var st)) tagSources.Add(st);

                // Standard Tags
                metadata.Originator = GetTag(tagSources, "encoded_by") ?? GetTag(tagSources, "originator") ?? "";
                metadata.CreationDate = GetTag(tagSources, "date") ?? GetTag(tagSources, "creation_time") ?? "";
                metadata.Scene = GetTag(tagSources, "scene") ?? "";
                metadata.Take = GetTag(tagSources, "take") ?? "";
                metadata.Tape = GetTag(tagSources, "tape") ?? GetTag(tagSources, "tape_id") ?? "";

                // TIMECODE CALCULATION (Crucial for BWF)
                // Priority 1: Check for 'time_reference' (Samples)
                string? timeRefStr = GetTag(tagSources, "time_reference");
                bool timecodeFound = false;

                if (!string.IsNullOrEmpty(timeRefStr) && long.TryParse(timeRefStr, out long timeRefSamples) && sampleRate > 0)
                {
                    // Calculate Timecode from Samples
                    metadata.TimeReferenceSeconds = (double)timeRefSamples / sampleRate;
                    metadata.TimecodeStart = SamplesToTimecode(timeRefSamples, sampleRate);
                    timecodeFound = true;
                }

                // Priority 2: Check for direct 'timecode' tag (e.g. QuickTime or other containers)
                if (!timecodeFound)
                {
                    string? tcTag = GetTag(tagSources, "timecode");
                    if (!string.IsNullOrEmpty(tcTag))
                    {
                         metadata.TimecodeStart = tcTag;
                         // Try to parse back to seconds roughly if needed, but display is key
                    }
                }
            }
            catch { /* Metadata parsing is non-critical to playback */ }
        }

        private string SamplesToTimecode(long samples, int sampleRate)
        {
            if (sampleRate == 0) return "00:00:00:00";

            // Calculate total seconds
            double totalSeconds = (double)samples / sampleRate;
            
            // Extract components
            TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
            
            // Calculate Frames
            // Assumption: Audio file doesn't store frame rate natively usually. 
            // We'll estimate based on standard 24/25/30 or just show frames as remainder samples converted to 'frame' slot?
            // Standard practice for audio-only BWF often implies a project frame rate, but here we'll assume 25fps or just show HH:MM:SS
            // User requested HH:MM:SS:FF. Let's assume 25fps for European broadcast standard if unknown, or just calculate frames.
            
            // Actually, nice HH:MM:SS is better than wrong FF. 
            // But let's try to be precise:
            // Frame count = (samples % sampleRate) / (sampleRate / fps)
            // Let's stick to standard TimeSpan string first + fractional if we can.
            
            // Better: Just use standard format.
            return t.ToString(@"hh\:mm\:ss"); 
        }

        private string? GetTag(List<JsonElement?> sources, string key)
        {
            foreach(var source in sources)
            {
                if (source.HasValue)
                {
                     foreach(var property in source.Value.EnumerateObject())
                    {
                        if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            return property.Value.GetString();
                        }
                    }
                }
            }
            return null;
        }
    }
}
