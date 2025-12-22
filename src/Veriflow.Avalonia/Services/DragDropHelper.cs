using Avalonia.Input;
using Avalonia.Platform.Storage; // Needed for IStorageItem
using System;
using System.Collections.Generic;
using System.Linq;

namespace Veriflow.Avalonia.Services;

/// <summary>
/// Helper class for handling drag-and-drop operations with Avalonia's new APIs
/// Wraps the new implementations to provide a cleaner interface
/// </summary>
public static class DragDropHelper
{
#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    /// Gets file paths from drag event
    /// </summary>
    public static IEnumerable<string> GetFiles(DragEventArgs e)
    {
        // Try to get files using the new StorageItem API first
        var files = e.Data.GetFiles();
        if (files != null)
        {
            var validPaths = new List<string>();
            foreach (var item in files)
            {
                if (item is IStorageFile file)
                {
                     validPaths.Add(file.Path.LocalPath);
                }
                else if (item is IStorageFolder folder)
                {
                     validPaths.Add(folder.Path.LocalPath);
                }
                // Fallback for generic IStorageItem if needed, Path property is on IStorageItem
                else if (item != null)
                {
                    validPaths.Add(item.Path.LocalPath);
                }
            }
            if (validPaths.Any()) return validPaths;
        }

        // Fallback or Old API check if needed (GetFileNames is the old one)
        if (e.Data.Contains(DataFormats.FileNames))
        {
             var oldFiles = e.Data.GetFileNames();
             if (oldFiles != null) return oldFiles;
        }
        
        return Enumerable.Empty<string>();
    }
    
    /// <summary>
    /// Checks if drag event contains files
    /// </summary>
    public static bool HasFiles(DragEventArgs e)
    {
        // Check for Files (StorageItems) or FileNames (String paths)
        return e.Data.Contains(DataFormats.Files) || e.Data.Contains(DataFormats.FileNames);
    }
    
    /// <summary>
    /// Gets the first file path from drag event, or null if none
    /// </summary>
    public static string? GetFirstFile(DragEventArgs e)
    {
        return GetFiles(e).FirstOrDefault();
    }
#pragma warning restore CS0618
}
