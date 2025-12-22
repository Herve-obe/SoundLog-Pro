using Avalonia.Controls;
using Avalonia.Input;
using Veriflow.Avalonia.ViewModels;
using Veriflow.Avalonia.Services;

namespace Veriflow.Avalonia.Views;

public partial class OffloadView : UserControl
{
    public OffloadView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is OffloadViewModel viewModel)
        {
            viewModel.RequestFolderPicker += OnRequestFolderPicker;
        }
    }

    private async void OnRequestFolderPicker(string target)
    {
        if (DataContext is not OffloadViewModel viewModel) return;
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return;

        var folder = await FilePickerService.PickFolderAsync(topLevel.StorageProvider);
        if (!string.IsNullOrEmpty(folder))
        {
            switch (target)
            {
                case "source":
                    viewModel.SourcePath = folder;
                    break;
                case "dest1":
                    viewModel.Destination1Path = folder;
                    break;
                case "dest2":
                    viewModel.Destination2Path = folder;
                    break;
            }
        }
    }

    // Drag & Drop Handlers for Source TextBox
    private void SourceTextBox_DragOver(object? sender, DragEventArgs e)
    {
        if (DragDropHelper.HasFiles(e))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void SourceTextBox_Drop(object? sender, DragEventArgs e)
    {
        var file = DragDropHelper.GetFirstFile(e);
        if (!string.IsNullOrEmpty(file) && DataContext is OffloadViewModel vm)
        {
            vm.SourcePath = file;
        }
    }

    // Drag & Drop Handlers for Destination 1 TextBox
    private void Dest1TextBox_DragOver(object? sender, DragEventArgs e)
    {
        if (DragDropHelper.HasFiles(e))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Dest1TextBox_Drop(object? sender, DragEventArgs e)
    {
        var file = DragDropHelper.GetFirstFile(e);
        if (!string.IsNullOrEmpty(file) && DataContext is OffloadViewModel vm)
        {
            vm.Destination1Path = file;
        }
    }

    // Drag & Drop Handlers for Destination 2 TextBox
    private void Dest2TextBox_DragOver(object? sender, DragEventArgs e)
    {
        if (DragDropHelper.HasFiles(e))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Dest2TextBox_Drop(object? sender, DragEventArgs e)
    {
        var file = DragDropHelper.GetFirstFile(e);
        if (!string.IsNullOrEmpty(file) && DataContext is OffloadViewModel vm)
        {
            vm.Destination2Path = file;
        }
    }
    // Drag & Drop Handlers for Verify TextBox
    private void VerifyTextBox_DragOver(object? sender, DragEventArgs e)
    {
        if (DragDropHelper.HasFiles(e))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void VerifyTextBox_Drop(object? sender, DragEventArgs e)
    {
        var file = DragDropHelper.GetFirstFile(e);
        if (!string.IsNullOrEmpty(file) && DataContext is OffloadViewModel vm)
        {
            vm.VerifyPath = file;
        }
    }
}
