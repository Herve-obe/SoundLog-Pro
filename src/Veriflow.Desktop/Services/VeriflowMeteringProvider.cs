using NAudio.Wave;
using System;

namespace Veriflow.Desktop.Services
{
    /// <summary>
    /// A Pass-Through SampleProvider that analyzes audio levels (peaks) for metering.
    /// </summary>
    public class VeriflowMeteringProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _maxSamples;


        public WaveFormat WaveFormat => _source.WaveFormat;

        /// <summary>
        /// Current peak values for each channel (0.0 to 1.0).
        /// </summary>
        public float[] ChannelPeaks { get; private set; }

        public VeriflowMeteringProvider(ISampleProvider source)
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

            // Reset current peaks for this block? 
            // In a real scenario, we might want to hold peaks for the UI refresh rate (e.g. 20-50ms).
            // However, since Read is called frequently, let's accumulate max and let the consumer read/reset it or just exposure "Instantaneous Peak" of the last buffer.
            // A better approach for smooth UI is to capture the max of this buffer.
            
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
            // We copy valid peaks. 
            Array.Copy(_maxSamples, ChannelPeaks, channels);

            return samplesRead;
        }
    }
}
