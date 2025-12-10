using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Veriflow.Desktop.ViewModels
{
    public partial class TranscodeViewModel : ObservableObject
    {
        private readonly Services.ITranscodingService _transcodingService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTranscodeCommand))]
        private string? _sourceFile;

        [ObservableProperty]
        private string? _destinationFolder;

        public ObservableCollection<string> AvailableFormats { get; } = new()
        {
            "WAV", "FLAC", "MP3", "AAC", "OGG"
        };

        [ObservableProperty]
        private string _selectedFormat = "WAV";

        public ObservableCollection<string> AvailableSampleRates { get; } = new()
        {
            "Same as Source", "44100", "48000", "88200", "96000"
        };

        [ObservableProperty]
        private string _selectedSampleRate = "Same as Source";

        public ObservableCollection<string> AvailableBitDepths { get; } = new()
        {
            "Same as Source", "16-bit", "24-bit", "32-bit Float"
        };

        [ObservableProperty]
        private string _selectedBitDepth = "Same as Source";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTranscodeCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public TranscodeViewModel()
        {
            // Ideally injected, but for now instantiation is fine or we can add to MainViewModel DI
            _transcodingService = new Services.TranscodingService(); 
        }

        [RelayCommand]
        private void PickSource()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Media Files|*.wav;*.mp3;*.flac;*.mp4;*.mov;*.mkv|All Files|*.*";
            if (dialog.ShowDialog() == true)
            {
                SourceFile = dialog.FileName;
                // Auto-set destination to same folder? Or leave empty? 
                if (string.IsNullOrEmpty(DestinationFolder))
                    DestinationFolder = System.IO.Path.GetDirectoryName(SourceFile);
            }
        }

        [RelayCommand]
        private void PickDestination()
        {
            // FolderPicker (using a hack or library, WPF default doesn't have a good one easily available without heavy config)
            // We'll use OpenFolderDialog if on newer .NET/Windows, else generic approach.
            // Simplified: Use OpenFileDialog to pick a "dummy" file or just assume user types it? 
            // Better: Use WindowsAPICodePack logic if available, but I don't have it.
            // Robust Fallback: OpenFileDialog with "CheckFileExists = false" and "FileName = Select Folder"
            
            // Actually, for this environment, let's assume we can paste it or use a simple folder logic.
            // Or try the OpenFolderDialog (available in .NET 8 / modern WPF).
             var dialog = new Microsoft.Win32.OpenFolderDialog(); 
             if (dialog.ShowDialog() == true)
             {
                 DestinationFolder = dialog.FolderName;
             }
        }

        [RelayCommand(CanExecute = nameof(CanStartTranscode))]
        private async Task StartTranscode()
        {
            if (IsBusy) return;
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (string.IsNullOrEmpty(DestinationFolder)) return;

            IsBusy = true;
            StatusMessage = "Transcoding...";
            ProgressValue = 0; // Indeterminate for now

            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(SourceFile);
                var extension = SelectedFormat.ToLower();
                var outputFile = System.IO.Path.Combine(DestinationFolder, $"{fileName}_transcoded.{extension}");

                var options = new Services.TranscodeOptions
                {
                    Format = SelectedFormat,
                    SampleRate = SelectedSampleRate,
                    BitDepth = SelectedBitDepth
                };

                await _transcodingService.TranscodeAsync(SourceFile, outputFile, options, null);
                
                StatusMessage = "Completed Successfully!";
                DisplayAlert("Success", $"Transcoding finished:\n{outputFile}");
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Error Occurred";
                DisplayAlert("Error", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanStartTranscode()
        {
            return !string.IsNullOrEmpty(SourceFile) && !IsBusy;
        }

        private void DisplayAlert(string title, string message)
        {
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
