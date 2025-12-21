using System;
using System.Collections.Generic;
using System.Linq;

namespace Veriflow.Avalonia.Services.Audio
{
    public class MixerEngine
    {
        private List<AudioTrack> _tracks = new();
        private float[] _mixBuffer;
        private int _bufferSize;
        private int _sampleRate;
        private int _channels;

        // Metering
        public float PeakL { get; private set; }
        public float PeakR { get; private set; }
        public float RmsL { get; private set; }
        public float RmsR { get; private set; }

        public MixerEngine(int sampleRate, int channels, int bufferSize = 1024)
        {
            _sampleRate = sampleRate;
            _channels = channels;
            _bufferSize = bufferSize;
            _mixBuffer = new float[bufferSize * channels];
        }

        public void AddTrack(AudioTrack track)
        {
            lock(_tracks)
            {
                _tracks.Add(track);
            }
        }

        public void RemoveTrack(AudioTrack track)
        {
            lock(_tracks)
            {
                _tracks.Remove(track);
            }
        }

        public void ClearTracks()
        {
             lock(_tracks)
            {
                foreach(var t in _tracks) t.Dispose();
                _tracks.Clear();
            }
        }

        public int Read(Span<float> output)
        {
            // Clear output (silence)
            output.Fill(0);

            float pL = 0;
            float pR = 0;
            float sumSqL = 0;
            float sumSqR = 0;

            lock(_tracks)
            {
                Span<float> trackBuffer = stackalloc float[output.Length];

                foreach (var track in _tracks)
                {
                    if (!track.IsPlaying) continue;

                    // Clear temp buffer? Or Read overwrites?
                    // Read overwrites/fills. If Read returns < length, rest is undefined?
                    // AudioTrack.Read returns count valid.
                    
                    int read = track.Read(trackBuffer);

                    // Mix
                    for (int i = 0; i < read; i+=_channels)
                    {
                        // Apply Volume/Pan
                        float vol = track.Volume;
                        // Simple Stereo Pan Law (-1L, 1R)
                        float pan = track.Pan;
                        float volL = vol * (pan <= 0 ? 1 : 1 - pan);
                        float volR = vol * (pan >= 0 ? 1 : 1 + pan);
                        
                        // Assuming stereo input for now
                        // If track is mono, map to stereo
                        float inL = trackBuffer[i];
                        float inR = (track.Channels > 1) ? trackBuffer[i+1] : trackBuffer[i];

                        output[i] += inL * volL;
                        output[i+1] += inR * volR;
                    }
                }
            }

            // Calculate Metering (post-mix)
            for (int i = 0; i < output.Length; i+=2)
            {
                float l = output[i];
                float r = output[i+1];

                float absL = Math.Abs(l);
                float absR = Math.Abs(r);

                if (absL > pL) pL = absL;
                if (absR > pR) pR = absR;

                sumSqL += l * l;
                sumSqR += r * r;
            }

            PeakL = 20 * (float)Math.Log10(pL + 1e-6); // dB
            PeakR = 20 * (float)Math.Log10(pR + 1e-6);
            
            // RMS over frame
            int frames = output.Length / 2;
            RmsL = 20 * (float)Math.Log10(Math.Sqrt(sumSqL / frames) + 1e-6);
            RmsR = 20 * (float)Math.Log10(Math.Sqrt(sumSqR / frames) + 1e-6);

            return output.Length;
        }

        // Just for reference, dB conversion
        // 20*log10(amp)
    }
}
