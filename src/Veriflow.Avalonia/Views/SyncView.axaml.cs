using Avalonia.Controls;
using Veriflow.Avalonia.ViewModels;
using Veriflow.Avalonia.Services;

namespace Veriflow.Avalonia.Views;

public partial class SyncView : UserControl
{
    public SyncView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is SyncViewModel viewModel)
        {
            viewModel.RequestFilePicker += OnRequestFilePicker;
        }
    }

    private async void OnRequestFilePicker(string target)
    {
        if (DataContext is not SyncViewModel viewModel) return;
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return;

        string? file = null;
        
        if (target == "video")
        {
            file = await FilePickerService.PickVideoFileAsync(topLevel.StorageProvider);
            if (!string.IsNullOrEmpty(file))
            {
                viewModel.VideoFile = file;
            }
        }
        else if (target == "audio")
        {
            file = await FilePickerService.PickAudioFileAsync(topLevel.StorageProvider);
            if (!string.IsNullOrEmpty(file))
            {
                viewModel.AudioFile = file;
            }
        }
    }
}
