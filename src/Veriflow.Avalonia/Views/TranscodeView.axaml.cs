using Avalonia.Controls;
using Veriflow.Avalonia.ViewModels;
using Veriflow.Avalonia.Services;
using System.Linq;
using Avalonia.Input;

namespace Veriflow.Avalonia.Views;

public partial class TranscodeView : UserControl
{
    public TranscodeView()
    {
        InitializeComponent();
        
        var dropZone = this.FindControl<Border>("QueueDropZone");
        if (dropZone != null)
        {
            dropZone.AddHandler(DragDrop.DragOverEvent, DragOver);
            dropZone.AddHandler(DragDrop.DropEvent, Drop);
        }
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        // TranscodeViewModel doesn't have RequestFilePicker yet
        // Will be added when needed
    }

    private async void OnRequestFilePicker()
    {
        if (DataContext is not TranscodeViewModel viewModel) return;
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return;

        var files = await FilePickerService.PickMultipleFilesAsync(topLevel.StorageProvider);
        if (files.Any())
        {
            viewModel.AddFiles(files);
        }
    }
    private void DragOver(object? sender, DragEventArgs e)
    {
        if (Services.DragDropHelper.HasFiles(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
        }
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is TranscodeViewModel vm)
        {
             vm.DropFilesCommand.Execute(e);
             await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
