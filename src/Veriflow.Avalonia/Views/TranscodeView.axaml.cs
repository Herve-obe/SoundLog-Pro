using Avalonia.Controls;
using Veriflow.Avalonia.ViewModels;
using Veriflow.Avalonia.Services;
using System.Linq;

namespace Veriflow.Avalonia.Views;

public partial class TranscodeView : UserControl
{
    public TranscodeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is TranscodeViewModel viewModel)
        {
            viewModel.RequestFilePicker += OnRequestFilePicker;
        }
    }

    private async void OnRequestFilePicker()
    {
        if (DataContext is not TranscodeViewModel viewModel) return;
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return;

        var files = await FilePickerService.PickMultipleFilesAsync(topLevel.StorageProvider);
        if (files.Any())
        {
            foreach (var file in files)
            {
                viewModel.AddFileToQueue(file);
            }
        }
    }
}
