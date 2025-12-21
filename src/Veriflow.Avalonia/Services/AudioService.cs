using System;
using System.IO;
using System.Runtime.InteropServices;
using MiniAudioEx;
using MiniAudioEx.Core;
using MiniAudioEx.Core.StandardAPI;
using MiniAudioEx.Core.AdvancedAPI;
using Veriflow.Avalonia.Services.Audio;
using System.Collections.Generic;

namespace Veriflow.Avalonia.Services
{
    // Testing MaDataSource
    public class MixerSource : MaDataSource
    {
        private MixerEngine _mixer;

        public MixerSource(MixerEngine mixer) : base() 
        {
            _mixer = mixer;
        }
        
        // public override void Read(IntPtr buffer, uint count) 
        // {
        //      // Probe
        // }
    }

    public class AudioService : IDisposable
    {
        private MixerEngine _mixer;
        private MixerSource _source;

        public bool IsPlaying { get; private set; }

        public AudioService()
        {
            _mixer = new MixerEngine(48000, 2);
            _source = new MixerSource(_mixer);
        }

        public void Play(string filePath)
        {
            Stop(); 
            
            var track = new AudioTrack(filePath);
            track.IsPlaying = true;
            _mixer.AddTrack(track);
            IsPlaying = true;
            
            // TODO: Hook _source to MiniAudio output once API is confirmed
            // e.g. AudioContext.Play(_source);
        }

        public void Stop()
        {
            _mixer.ClearTracks();
            IsPlaying = false;
        }

        public void Dispose()
        {
            if (_source is IDisposable d) d.Dispose();
        }
        
        public (float pL, float pR) GetPeaks()
        {
             return (_mixer?.PeakL ?? 0, _mixer?.PeakR ?? 0);
        }
    }
}
