using Avalonia.Controls;
using Avalonia.Input;
using Veriflow.Avalonia.ViewModels;
using Veriflow.Avalonia.Models;

namespace Veriflow.Avalonia.Views;

public partial class FileExplorerView : UserControl
{
    public FileExplorerView()
    {
        InitializeComponent();
        DataContext = new FileExplorerViewModel();
    }

    private async void TreeItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Only start drag on left button press
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && 
            sender is StackPanel panel &&
            panel.DataContext is DirectoryNode node &&
            !string.IsNullOrEmpty(node.FullPath))
        {
#pragma warning disable CS0618
            var dragData = new DataObject();
            dragData.Set(DataFormats.Text, node.FullPath);
            
            await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
#pragma warning restore CS0618
        }
    }
}
