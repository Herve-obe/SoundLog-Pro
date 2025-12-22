using Avalonia.Controls;
using Veriflow.Avalonia.ViewModels;
using Veriflow.Avalonia.Services;

namespace Veriflow.Avalonia.Views
{
    public partial class AudioPlayerView : UserControl
    {
        public AudioPlayerView()
        {
            InitializeComponent();
            
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is AudioPlayerViewModel viewModel)
            {
                viewModel.RequestFilePicker += OnRequestFilePicker;
            }
        }

        private async void OnRequestFilePicker()
        {
            if (DataContext is not AudioPlayerViewModel viewModel) return;
            
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var file = await FilePickerService.PickAudioFileAsync(topLevel.StorageProvider);
            if (!string.IsNullOrEmpty(file))
            {
                await viewModel.LoadAudio(file);
            }
        }
    }
}
