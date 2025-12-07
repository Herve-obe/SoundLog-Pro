using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace Veriflow.Desktop.Services
{
    /// <summary>
    /// Custom ISampleProvider that allows individual volume and mute control for each channel.
    /// </summary>
    public class MultiChannelAudioMixer : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _channelVolumes;
        private readonly bool[] _channelMutes;
        private readonly int _channels;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public MultiChannelAudioMixer(ISampleProvider source)
        {
            _source = source;
            _channels = source.WaveFormat.Channels;
            _channelVolumes = new float[_channels];
            _channelMutes = new bool[_channels];

            // Default: All channels active, full volume
            for (int i = 0; i < _channels; i++)
            {
                _channelVolumes[i] = 1.0f;
                _channelMutes[i] = false;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            // Apply per-channel processing
            for (int i = 0; i < samplesRead; i++)
            {
                int channelIndex = (i + offset) % _channels; // Determine which channel this sample belongs to

                if (_channelMutes[channelIndex])
                {
                    buffer[i + offset] = 0.0f;
                }
                else
                {
                    buffer[i + offset] *= _channelVolumes[channelIndex];
                }
            }

            return samplesRead;
        }

        public void SetChannelVolume(int channel, float volume)
        {
            if (channel >= 0 && channel < _channels)
            {
                _channelVolumes[channel] = volume;
            }
        }

        public void SetChannelMute(int channel, bool isMuted)
        {
            if (channel >= 0 && channel < _channels)
            {
                _channelMutes[channel] = isMuted;
            }
        }
    }
}
