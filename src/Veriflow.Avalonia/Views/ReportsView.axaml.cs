using Avalonia.Controls;
using Avalonia.Input;

namespace Veriflow.Avalonia.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
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
        if (DataContext is ViewModels.ReportsViewModel vm)
        {
             await vm.DropFileCommand.ExecuteAsync(e);
        }
    }
}
