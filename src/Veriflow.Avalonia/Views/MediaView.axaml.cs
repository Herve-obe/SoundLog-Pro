using Avalonia.Controls;
using Avalonia.Input;
using Veriflow.Avalonia.ViewModels;
using System.Linq;

namespace Veriflow.Avalonia.Views;

public partial class MediaView : UserControl
{
    public MediaView()
    {
        InitializeComponent();
        
        var dropZone = this.FindControl<Border>("FileDropZone");
        if (dropZone != null)
        {
            dropZone.AddHandler(DragDrop.DragOverEvent, DragOver);
            dropZone.AddHandler(DragDrop.DropEvent, Drop);
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
         if (DataContext is MediaViewModel vm)
         {
              vm.DropMediaCommand.Execute(e);
              await System.Threading.Tasks.Task.CompletedTask;
         }
    }
}
