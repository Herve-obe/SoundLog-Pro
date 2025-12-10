using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams;
using System;
using System.IO;

namespace Veriflow.Desktop.Services
{
    public class AudioPreviewService : IDisposable
    {
        private ISoundOut? _outputDevice;
        private IWaveSource? _audioSource;

        public void Play(string filePath)
        {
            Stop(); // Ensure any previous playback is stopped

            try
            {
                if (!File.Exists(filePath)) return;

                // 1. Get Source (Supports WAV, FLAC, RF64, MP3 via MediaFoundation/Codecs)
                _audioSource = CodecFactory.Instance.GetCodec(filePath);

                // 2. Mono Downmix Logic (Preview on both speakers)
                if (_audioSource.WaveFormat.Channels > 1)
                {
                    // Convert to SampleSource to manipulate data
                    var sampleSource = _audioSource.ToSampleSource();
                    
                    // Wrap in our custom Mono Mixer
                    var monoSource = new MonoSampleDownmixer(sampleSource);
                    
                    // Convert back to WaveSource for SoundOut (keeping sample rate, but Mono)
                    // Actually, SoundOut handles Mono fine (plays on both usually) or we replicate to Stereo.
                    // For "Preview on both speakers" from a Mono source, Wasapi typically maps Mono to L+R.
                    // So simply making it 1 Channel is enough.
                    _audioSource = monoSource.ToWaveSource();
                }

                // 3. Init Output (WasapiOut for Pro/Low Latency)
                _outputDevice = new WasapiOut();
                _outputDevice.Initialize(_audioSource);
                _outputDevice.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing preview: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Stop();
                _outputDevice.Dispose();
                _outputDevice = null;
            }

            if (_audioSource != null)
            {
                _audioSource.Dispose();
                _audioSource = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        // Simple Downmixer: Takes [L, R, ...] and outputs [Avg]
        private class MonoSampleDownmixer : ISampleSource
        {
            private readonly ISampleSource _source;
            public WaveFormat WaveFormat { get; }

            public bool CanSeek => _source.CanSeek;
            public long Position
            {
                get => _source.Position / _source.WaveFormat.Channels;
                set => _source.Position = value * _source.WaveFormat.Channels;
            }
            public long Length => _source.Length / _source.WaveFormat.Channels;

            public MonoSampleDownmixer(ISampleSource source)
            {
                _source = source;
                if (source.WaveFormat.Channels == 1)
                    throw new ArgumentException("Source is already Mono");

                // Output is Mono, IEEE Float (32-bit)
                WaveFormat = new WaveFormat(source.WaveFormat.SampleRate, 32, 1, AudioEncoding.IeeeFloat);
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int inputChannels = _source.WaveFormat.Channels;
                int sourceSamplesToRead = count * inputChannels;
                float[] sourceBuffer = new float[sourceSamplesToRead];

                int read = _source.Read(sourceBuffer, 0, sourceSamplesToRead);

                int outputSamples = read / inputChannels;

                for (int i = 0; i < outputSamples; i++)
                {
                    float sum = 0;
                    for (int ch = 0; ch < inputChannels; ch++)
                    {
                        sum += sourceBuffer[i * inputChannels + ch];
                    }
                    buffer[offset + i] = sum / inputChannels;
                }

                return outputSamples;
            }

            public void Dispose()
            {
                (_source as IDisposable)?.Dispose();
            }
        }
    }
}
