using Avalonia.Controls;
using Avalonia.Input;

namespace Veriflow.Avalonia.Views;

public partial class SyncView : UserControl
{
    public SyncView()
    {
        InitializeComponent();
        
        var videoZone = this.FindControl<Border>("VideoDropZone");
        if (videoZone != null)
        {
            videoZone.AddHandler(DragDrop.DragOverEvent, DragOver);
            videoZone.AddHandler(DragDrop.DropEvent, VideoDrop);
        }

        var audioZone = this.FindControl<Border>("AudioDropZone");
        if (audioZone != null)
        {
            audioZone.AddHandler(DragDrop.DragOverEvent, DragOver);
            audioZone.AddHandler(DragDrop.DropEvent, AudioDrop);
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

    private async void VideoDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is ViewModels.SyncViewModel vm)
        {
             await vm.DropVideoCommand.ExecuteAsync(e);
        }
    }
    
    private async void AudioDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is ViewModels.SyncViewModel vm)
        {
             await vm.DropAudioCommand.ExecuteAsync(e);
        }
    }
}
