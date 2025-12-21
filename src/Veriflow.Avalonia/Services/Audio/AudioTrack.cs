using System;
using System.IO;
using NLayer;
// using MiniAudioEx; // Will add later if needed for decoding

namespace Veriflow.Avalonia.Services.Audio
{
    /// <summary>
    /// Represents a single audio track (file) that can be played by the mixer.
    /// Handles decoding (MP3 via NLayer, WAV via simple reader or future MiniAudio decoder).
    /// </summary>
    public class AudioTrack : IDisposable
    {
        private MpegFile? _mp3Reader;
        private FileStream? _fileStream;
        private BinaryReader? _binaryReader;
        
        public string FilePath { get; }
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }
        public long TotalSamples { get; private set; }
        public long Position { get; private set; } // In samples (frames)

        public bool IsPlaying { get; set; }
        public bool IsLooping { get; set; }
        public float Volume { get; set; } = 1.0f;
        public float Pan { get; set; } = 0.0f; // -1.0 to 1.0

        private float[] _readBuffer = new float[4096];
        private readonly object _lock = new object();

        // Wav header info
        private int _dataChunkPos;
        private int _bytesPerSample;

        public AudioTrack(string filePath)
        {
            FilePath = filePath;
            LoadFile();
        }

        private void LoadFile()
        {
            var ext = Path.GetExtension(FilePath).ToLower();
            if (ext == ".mp3")
            {
                _mp3Reader = new MpegFile(FilePath);
                SampleRate = _mp3Reader.SampleRate;
                Channels = _mp3Reader.Channels;
                TotalSamples = _mp3Reader.Length; // Length in samples
            }
            else if (ext == ".wav")
            {
                ParseWav(FilePath);
            }
            else
            {
                throw new NotSupportedException($"Format {ext} not supported yet in managed AudioTrack. (Use MiniAudio decoder)");
            }
        }

        private void ParseWav(string path)
        {
            // Simple WAV parser
            _fileStream = File.OpenRead(path);
            _binaryReader = new BinaryReader(_fileStream);

            // RIFF header
            if (new string(_binaryReader.ReadChars(4)) != "RIFF") throw new Exception("Not a WAV file");
            _binaryReader.ReadInt32(); // File size
            if (new string(_binaryReader.ReadChars(4)) != "WAVE") throw new Exception("Not a WAVE file");

            // Chunks
            while (_fileStream.Position < _fileStream.Length)
            {
                var chunkId = new string(_binaryReader.ReadChars(4));
                var chunkSize = _binaryReader.ReadInt32();
                var pos = _fileStream.Position;

                if (chunkId == "fmt ")
                {
                    var format = _binaryReader.ReadInt16(); // 1=PCM, 3=Float
                    Channels = _binaryReader.ReadInt16();
                    SampleRate = _binaryReader.ReadInt32();
                    _binaryReader.ReadInt32(); // Byte rate
                    _binaryReader.ReadInt16(); // Block align
                    var bits = _binaryReader.ReadInt16();
                    _bytesPerSample = bits / 8;
                }
                else if (chunkId == "data")
                {
                    _dataChunkPos = (int)pos;
                    TotalSamples = chunkSize / (Channels * _bytesPerSample); // Interleaved frames
                    break;
                }

                _fileStream.Position = pos + chunkSize;
            }

            _fileStream.Position = _dataChunkPos;
        }

        /// <summary>
        /// Reads floating point samples into the buffer.
        /// Returns number of samples read.
        /// </summary>
        /// <param name="buffer">Interleaved buffer</param>
        /// <param name="count">Number of samples (frames * channels) to read</param>
        public int Read(Span<float> buffer)
        {
            lock (_lock)
            {
                if (!IsPlaying) return 0;

                int readCount = 0;

                if (_mp3Reader != null)
                {
                    readCount = _mp3Reader.ReadSamples(buffer.ToArray(), 0, buffer.Length); // NLayer uses array
                    // Copy back if needed or optimize later
                    // Correction: NLayer ReadSamples returns int.
                    // Wait, NLayer ReadSamples takes float[] buffer.
                    // Span not supported directly.
                    // We need a temp array or unsafe.
                    // For now, use array.
                    var temp = new float[buffer.Length];
                    readCount = _mp3Reader.ReadSamples(temp, 0, buffer.Length);
                    for(int i=0; i<readCount; i++) buffer[i] = temp[i];
                }
                else if (_fileStream != null)
                {
                    // Read WAV bytes and convert to float
                    // Assuming PCM 16 or 24 or 32
                    // Quick implementation for PCM 16
                    int bytesToRead = buffer.Length * _bytesPerSample;
                    byte[] bytes = new byte[bytesToRead];
                    int bytesRead = _fileStream.Read(bytes, 0, bytesToRead);
                    readCount = bytesRead / _bytesPerSample;

                    for (int i = 0; i < readCount; i++)
                    {
                        if (_bytesPerSample == 2) // 16 bit
                        {
                            short val = BitConverter.ToInt16(bytes, i * 2);
                            buffer[i] = val / 32768f;
                        }
                        else if (_bytesPerSample == 3) // 24 bit
                        {
                            // unpack 24 bit
                            int val = (bytes[i*3] << 8) | (bytes[i*3+1] << 16) | (bytes[i*3+2] << 24);
                            val >>= 8; 
                            buffer[i] = val / 8388608f;
                        }
                         else if (_bytesPerSample == 4) // 32 bit float (assuming float type 3 in header)
                        {
                             buffer[i] = BitConverter.ToSingle(bytes, i * 4);
                        }
                    }
                }
                
                Position += readCount / Channels;
                
                if (readCount == 0 && IsLooping)
                {
                    Seek(0);
                    // Read again?
                }

                return readCount;
            }
        }

        public void Seek(long frame)
        {
            lock (_lock)
            {
                 Position = frame;
                 if (_mp3Reader != null)
                 {
                     _mp3Reader.Time = TimeSpan.FromSeconds((double)frame / SampleRate);
                 }
                 else if (_fileStream != null)
                 {
                     long bytePos = _dataChunkPos + (frame * Channels * _bytesPerSample);
                     _fileStream.Position = bytePos;
                 }
            }
        }

        public void Dispose()
        {
            _mp3Reader?.Dispose();
            _fileStream?.Dispose();
            _binaryReader?.Dispose();
        }
    }
}
