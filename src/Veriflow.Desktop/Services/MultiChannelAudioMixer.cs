using CSCore;
using System;
using System.Linq;

namespace Veriflow.Desktop.Services
{
    /// <summary>
    /// Custom ISampleSource that downmixes multi-channel input to Stereo output
    /// and allows individual volume/mute control + Panning.
    /// </summary>
    public class MultiChannelAudioMixer : ISampleSource
    {
        private readonly ISampleSource _source;
        private readonly float[] _channelVolumes;
        private readonly bool[] _channelMutes;
        private readonly bool[] _channelSolos;
        private readonly float[] _channelPans;
        private readonly int _inputChannels;
        private float[] _sourceBuffer;

        // Force Stereo Output (IEEE Float)
        public WaveFormat WaveFormat { get; }
        
        // Metadata / Navigation
        public bool CanSeek => _source.CanSeek;
        
        public long Position
        {
            get => ConvertInputToOutput(_source.Position);
            set => _source.Position = ConvertOutputToInput(value);
        }

        public long Length => ConvertInputToOutput(_source.Length);

        public MultiChannelAudioMixer(ISampleSource source)
        {
            _source = source;
            _inputChannels = source.WaveFormat.Channels;
            
            // Output is always Stereo, same SampleRate
            WaveFormat = new WaveFormat(_source.WaveFormat.SampleRate, 32, 2, AudioEncoding.IeeeFloat);

            _channelVolumes = new float[_inputChannels];
            _channelMutes = new bool[_inputChannels];
            _channelSolos = new bool[_inputChannels];
            _channelPans = new float[_inputChannels];
            
            // Buffer to hold raw input samples before downmix
            _sourceBuffer = new float[_inputChannels * 1024]; 

            // Default: All channels active, full volume, Center pan
            for (int i = 0; i < _inputChannels; i++)
            {
                _channelVolumes[i] = 1.0f;
                _channelMutes[i] = false;
                _channelSolos[i] = false;
                _channelPans[i] = 0.0f; // Center
            }
        }
        
        private long ConvertInputToOutput(long bytes)
        {
            if (_source.WaveFormat.BlockAlign == 0) return 0;
            long frames = bytes / _source.WaveFormat.BlockAlign;
            return frames * WaveFormat.BlockAlign;
        }

        private long ConvertOutputToInput(long bytes)
        {
             if (WaveFormat.BlockAlign == 0) return 0;
             long frames = bytes / WaveFormat.BlockAlign;
             return frames * _source.WaveFormat.BlockAlign;
        }

        public void SetChannelPan(int channel, float pan)
        {
            if (channel >= 0 && channel < _inputChannels)
            {
                if (pan < -1.0f) pan = -1.0f;
                if (pan > 1.0f) pan = 1.0f;
                _channelPans[channel] = pan;
            }
        }

        public void SetChannelVolume(int channel, float volume)
        {
            if (channel >= 0 && channel < _inputChannels)
            {
                _channelVolumes[channel] = volume;
            }
        }

        public void SetChannelMute(int channel, bool muted)
        {
            if (channel >= 0 && channel < _inputChannels)
            {
                _channelMutes[channel] = muted;
            }
        }

        public void SetChannelSolo(int channel, bool soloed)
        {
             if (channel >= 0 && channel < _inputChannels)
            {
                _channelSolos[channel] = soloed;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // 'count' is samples
            int outputFrames = count / 2;
            int samplesToReadFromSource = outputFrames * _inputChannels;

            // Ensure source buffer is big enough
            if (_sourceBuffer.Length < samplesToReadFromSource)
            {
                _sourceBuffer = new float[samplesToReadFromSource];
            }

            int sourceSamplesRead = _source.Read(_sourceBuffer, 0, samplesToReadFromSource);
            int framesRead = sourceSamplesRead / _inputChannels;

            bool anySolo = false;
            for (int i = 0; i < _inputChannels; i++)
            {
                if (_channelSolos[i])
                {
                    anySolo = true;
                    break;
                }
            }

            int outIndex = offset;
            
            for (int frame = 0; frame < framesRead; frame++)
            {
                float sumLeft = 0;
                float sumRight = 0;
                int inputOffset = frame * _inputChannels;

                for (int ch = 0; ch < _inputChannels; ch++)
                {
                    bool isAudible = true;

                    if (_channelMutes[ch])
                    {
                        isAudible = false;
                    }
                    else if (anySolo && !_channelSolos[ch])
                    {
                        isAudible = false;
                    }

                    if (isAudible)
                    {
                        float sample = _sourceBuffer[inputOffset + ch];
                        float vol = _channelVolumes[ch];
                        float pan = _channelPans[ch]; 
                        
                        float gainLeft = (1.0f - pan) / 2.0f;
                        float gainRight = (1.0f + pan) / 2.0f;

                        float processed = sample * vol;
                        
                        sumLeft += processed * gainLeft;
                        sumRight += processed * gainRight;
                    }
                }

                buffer[outIndex++] = sumLeft; 
                buffer[outIndex++] = sumRight;
            }

            return framesRead * 2;
        }
        
        public void Dispose()
        {
             (_source as IDisposable)?.Dispose();
        }
    }
}
