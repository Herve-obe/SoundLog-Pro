using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Veriflow.Avalonia.Services;

public class FilePickerService
{
    public static async Task<string?> PickAudioFileAsync(IStorageProvider storageProvider)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio Files")
                {
                    Patterns = new[] { "*.wav", "*.mp3", "*.aif", "*.aiff", "*.flac", "*.m4a", "*.aac", "*.ogg" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return files?.FirstOrDefault()?.Path.LocalPath;
    }

    public static async Task<string?> PickVideoFileAsync(IStorageProvider storageProvider)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Video File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video Files")
                {
                    Patterns = new[] { "*.mov", "*.mp4", "*.avi", "*.mkv", "*.mxf", "*.m4v" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return files?.FirstOrDefault()?.Path.LocalPath;
    }

    public static async Task<IReadOnlyList<string>> PickMultipleFilesAsync(IStorageProvider storageProvider)
    {
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Files",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Media Files")
                {
                    Patterns = new[] { "*.wav", "*.mp3", "*.mov", "*.mp4", "*.avi", "*.mkv" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return files?.Select(f => f.Path.LocalPath).ToList() ?? new List<string>();
    }

    public static async Task<string?> PickFolderAsync(IStorageProvider storageProvider)
    {
        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        return folders?.FirstOrDefault()?.Path.LocalPath;
    }
}
