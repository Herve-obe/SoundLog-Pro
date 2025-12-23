using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Veriflow.Avalonia.Models;

namespace Veriflow.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the file explorer component.
/// Manages directory tree with lazy loading.
/// </summary>
public partial class FileExplorerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DirectoryNode> _rootNodes = new();

    [ObservableProperty]
    private string? _selectedDirectory;

    public FileExplorerViewModel()
    {
        LoadDrives();
    }

    /// <summary>
    /// Loads all logical drives as root nodes.
    /// </summary>
    private void LoadDrives()
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .OrderBy(d => d.Name);

            foreach (var drive in drives)
            {
                var node = new DirectoryNode(
                    name: $"{drive.Name} ({drive.VolumeLabel})",
                    fullPath: drive.RootDirectory.FullName
                );

                // Set node type based on drive type
                node.NodeType = drive.DriveType switch
                {
                    DriveType.Fixed => DirectoryNodeType.FixedDrive,
                    DriveType.Removable => DirectoryNodeType.RemovableDrive,
                    DriveType.Network => DirectoryNodeType.NetworkDrive,
                    DriveType.CDRom => DirectoryNodeType.CDRom,
                    _ => DirectoryNodeType.Folder
                };
                
                node.PropertyChanged += Node_PropertyChanged;
                RootNodes.Add(node);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading drives: {ex.Message}");
        }
    }

    private void Node_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not DirectoryNode node) return;

        if (e.PropertyName == nameof(DirectoryNode.IsExpanded) && node.IsExpanded)
        {
            // Lazy load children when expanded (only if not already loaded)
            if (node.HasPlaceholder)
            {
                System.Diagnostics.Debug.WriteLine($"Expanding node: {node.Name}, HasPlaceholder: {node.HasPlaceholder}");
                LoadChildren(node);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Node already loaded: {node.Name}, Children count: {node.Children.Count}");
            }
        }
        else if (e.PropertyName == nameof(DirectoryNode.IsSelected) && node.IsSelected)
        {
            // Update selected directory
            SelectedDirectory = node.FullPath;
        }
    }

    /// <summary>
    /// Loads subdirectories for a given node (lazy loading).
    /// </summary>
    private void LoadChildren(DirectoryNode parent)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"LoadChildren called for: {parent.FullPath}");
            
            // Remove placeholder
            parent.Children.Clear();

            var directory = new DirectoryInfo(parent.FullPath);
            var subdirectories = directory.GetDirectories()
                .OrderBy(d => d.Name);

            System.Diagnostics.Debug.WriteLine($"Found {subdirectories.Count()} subdirectories");

            foreach (var subdir in subdirectories)
            {
                // Skip hidden and system directories
                if ((subdir.Attributes & FileAttributes.Hidden) != 0 ||
                    (subdir.Attributes & FileAttributes.System) != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping hidden/system: {subdir.Name}");
                    continue;
                }

                try
                {
                    // Create child node without testing accessibility upfront
                    // The accessibility test will happen when the user tries to expand this folder
                    var childNode = new DirectoryNode(subdir.Name, subdir.FullName);
                    childNode.PropertyChanged += Node_PropertyChanged;
                    parent.Children.Add(childNode);
                    System.Diagnostics.Debug.WriteLine($"Added folder: {subdir.Name}");
                }
                catch (UnauthorizedAccessException)
                {
                    System.Diagnostics.Debug.WriteLine($"Access denied to: {subdir.Name}");
                    continue;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error accessing {subdir.FullName}: {ex.Message}");
                    continue;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total children added: {parent.Children.Count}");

            // If no children, add a placeholder to indicate empty folder
            if (parent.Children.Count == 0)
            {
                parent.Children.Add(new DirectoryNode("(Empty)", string.Empty));
            }
        }
        catch (UnauthorizedAccessException)
        {
            parent.Children.Clear();
            parent.Children.Add(new DirectoryNode("(Access Denied)", string.Empty));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading children for {parent.FullPath}: {ex.Message}");
            parent.Children.Clear();
            parent.Children.Add(new DirectoryNode("(Error)", string.Empty));
        }
    }
}
