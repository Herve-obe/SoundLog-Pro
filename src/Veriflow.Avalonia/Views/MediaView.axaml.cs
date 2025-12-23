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

    private void Thumbnail_PointerEnter(object? sender, PointerEventArgs e)
    {
        // Show play overlay on hover
        if (sender is Border outerBorder)
        {
            var stackPanel = outerBorder.Child as StackPanel;
            var thumbnailBorder = stackPanel?.Children.OfType<Border>().FirstOrDefault(b => b.Name == "ThumbnailBorder");
            var grid = thumbnailBorder?.Child as Grid;
            var playOverlay = grid?.Children.OfType<Border>().FirstOrDefault(b => b.Name == "PlayOverlay");
            
            if (playOverlay != null)
            {
                playOverlay.IsVisible = true;
            }
        }
    }

    private void Thumbnail_PointerLeave(object? sender, PointerEventArgs e)
    {
        // Hide play overlay when not hovering
        if (sender is Border outerBorder)
        {
            var stackPanel = outerBorder.Child as StackPanel;
            var thumbnailBorder = stackPanel?.Children.OfType<Border>().FirstOrDefault(b => b.Name == "ThumbnailBorder");
            var grid = thumbnailBorder?.Child as Grid;
            var playOverlay = grid?.Children.OfType<Border>().FirstOrDefault(b => b.Name == "PlayOverlay");
            
            if (playOverlay != null)
            {
                playOverlay.IsVisible = false;
            }
        }
    }

    private void Thumbnail_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[MediaView] Thumbnail (Grid) clicked - SELECT");
        
        // Click on Grid background (not play button) → Select media
        if (sender is Grid grid && grid.Parent is Border thumbnailBorder)
        {
            if (thumbnailBorder.Parent is StackPanel stackPanel && stackPanel.Parent is Border outerBorder)
            {
                if (outerBorder.DataContext is MediaItemViewModel item)
                {
                    var vm = DataContext as MediaViewModel;
                    vm?.SelectMediaCommand.Execute(item);
                    System.Diagnostics.Debug.WriteLine($"[MediaView] Selected: {item.Name}");
                }
            }
        }
    }
    
    private void PlayCircle_PointerEnter(object? sender, PointerEventArgs e)
    {
        // Stage 2: White circle on play button hover
        if (sender is global::Avalonia.Controls.Shapes.Ellipse circle)
        {
            circle.Fill = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Colors.White);
            System.Diagnostics.Debug.WriteLine("[MediaView] Play circle hover - WHITE");
        }
    }
    
    private void PlayCircle_PointerExit(object? sender, PointerEventArgs e)
    {
        // Remove white circle
        if (sender is global::Avalonia.Controls.Shapes.Ellipse circle)
        {
            circle.Fill = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Colors.Transparent);
        }
    }

    private void PlayButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[MediaView] Play button clicked - PREVIEW");
        
        // Click on play button → Launch preview
        e.Handled = true; // Prevent Grid click from firing
        
        if (sender is Border playOverlay && playOverlay.Parent is Grid grid && grid.Parent is Border thumbnailBorder)
        {
            if (thumbnailBorder.Parent is StackPanel stackPanel && stackPanel.Parent is Border outerBorder)
            {
                if (outerBorder.DataContext is MediaItemViewModel item)
                {
                    var vm = DataContext as MediaViewModel;
                    _ = vm?.PreviewFileCommand.ExecuteAsync(item);
                }
            }
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
