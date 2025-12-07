using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Media;

namespace Veriflow.Desktop.ViewModels
{
    public partial class TrackViewModel : ObservableObject
    {
        public int ChannelIndex { get; }
        private readonly Action<TrackViewModel> _onSoloRequested;
        private readonly Action<int, float> _onVolumeChanged;
        private readonly Action<int, bool> _onMuteChanged;

        [ObservableProperty]
        private string _trackName;

        [ObservableProperty]
        private float _volume = 1.0f;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActive))]
        private bool _isMuted;

        [ObservableProperty]
        private bool _isSoloed;
        
        // Visuals
        [ObservableProperty]
        private PointCollection _waveformPoints = new();

        public bool IsActive => !IsMuted;

        public TrackViewModel(int channelIndex, string defaultName, Action<TrackViewModel> onSoloRequested, Action<int, float> onVolumeChanged, Action<int, bool> onMuteChanged)
        {
            ChannelIndex = channelIndex;
            TrackName = defaultName;
            _onSoloRequested = onSoloRequested;
            _onVolumeChanged = onVolumeChanged;
            _onMuteChanged = onMuteChanged;
        }

        partial void OnVolumeChanged(float value)
        {
            _onVolumeChanged?.Invoke(ChannelIndex, value);
        }

        partial void OnIsMutedChanged(bool value)
        {
            // If we are muted manually, we just notify mixer.
            // Solo logic might force-mute us, but that's handled by parent setting this Generic 'IsMuted' or a separate 'IsSoloMuted' flag?
            // To keep it simple: IsMuted reflects the User's Mute button. 
            // The Mixer might need a separate 'SoloMute' state, or we update IsMuted.
            // Requirement: "Solo on one track must mute all others".
            // Implementation: The Parent (PlayerVM) will update the Mixer directly for Solo logic.
            // BUT: The UI mute button should probably reflect state? Or stay independent?
            // Standard console: Mute and Solo are separate states. If not Soloed and another track IS Soloed, this track is effectively muted but the Mute button doesn't toggle.
            // We need a way to tell the View "You are effectively muted".
            // Let's rely on the Mixer doing the work. This IsMuted is USER mute.
            _onMuteChanged?.Invoke(ChannelIndex, value);
        }

        [RelayCommand]
        private void ToggleSolo()
        {
            IsSoloed = !IsSoloed;
            _onSoloRequested?.Invoke(this);
        }

        [RelayCommand]
        private void ToggleMute()
        {
            IsMuted = !IsMuted;
        }
    }
}
