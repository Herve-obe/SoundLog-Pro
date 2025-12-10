using CSCore;
using System;

namespace Veriflow.Desktop.Services
{
    /// <summary>
    /// A Pass-Through SampleSource that analyzes audio levels (peaks) for metering.
    /// </summary>
    public class VeriflowMeteringProvider : ISampleSource
    {
        private readonly ISampleSource _source;
        private readonly float[] _maxSamples;

        public WaveFormat WaveFormat => _source.WaveFormat;
        
        // ISampleSource properties
        public bool CanSeek => _source.CanSeek;
        public long Position 
        { 
            get => _source.Position; 
            set => _source.Position = value; 
        }
        public long Length => _source.Length;

        /// <summary>
        /// Current peak values for each channel (0.0 to 1.0).
        /// </summary>
        public float[] ChannelPeaks { get; private set; }

        public VeriflowMeteringProvider(ISampleSource source)
        {
            _source = source;
            int channels = source.WaveFormat.Channels;
            _maxSamples = new float[channels];
            ChannelPeaks = new float[channels];
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            // Analyze peaks
            int channels = WaveFormat.Channels;

            // Clear local max buffer
            Array.Clear(_maxSamples, 0, _maxSamples.Length);

            for (int i = 0; i < samplesRead; i += channels)
            {
                for (int c = 0; c < channels; c++)
                {
                    if (i + c < samplesRead)
                    {
                        float sample = Math.Abs(buffer[offset + i + c]);
                        if (sample > _maxSamples[c])
                        {
                            _maxSamples[c] = sample;
                        }
                    }
                }
            }

            // Update public property
            Array.Copy(_maxSamples, ChannelPeaks, channels);

            return samplesRead;
        }
        
        public void Dispose()
        {
            // Do not dispose source here usually, or yes? 
            // Aggregators usually don't own source life unless specified.
            // But usually chains dispose down.
            // Safe to not dispose _source if handled by player, but let's follow pattern.
            // If we assume ownership, we dispose.
            (_source as IDisposable)?.Dispose(); 
        }
    }
}
