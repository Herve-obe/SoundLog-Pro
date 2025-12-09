using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Veriflow.Desktop.Services;
using System.Windows.Input;

namespace Veriflow.Desktop.ViewModels
{
    public partial class MediaViewModel : ObservableObject
    {
        private readonly AudioPreviewService _audioService = new();

        [ObservableProperty]
        private ObservableCollection<DriveViewModel> _drives = new();

        [ObservableProperty]
        private MediaItemViewModel? _selectedMedia;

        [ObservableProperty]
        private bool _isPreviewing;
        
        [ObservableProperty]
        private string _currentPath = @"C:\";

        [ObservableProperty]
        private ObservableCollection<MediaItemViewModel> _fileList = new();

        private System.Timers.Timer _driveWatcher;

        public MediaViewModel()
        {
            // Initial Drive Load
            RefreshDrives();

            // Setup Drive Polling (Hot-Plug Support)
            _driveWatcher = new System.Timers.Timer(2000); // Check every 2 seconds
            _driveWatcher.Elapsed += (s, e) => 
            {
                // Dispatch to UI thread if drives changed
                try 
                {
                    var currentDrives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.Name).OrderBy(n => n).ToList();
                    var loadedDrives = Drives.Select(d => d.Path).OrderBy(n => n).ToList();
                    
                    if (!currentDrives.SequenceEqual(loadedDrives))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(RefreshDrives);
                    }
                }
                catch {}
            };
            _driveWatcher.Start();
        }

        private void RefreshDrives()
        {
            try
            {
                var currentDrives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();

                // Simple Strategy: Rebuild list to avoid complex sync logic for now, or merge.
                // Rebuilding is safer for clearing disconnected drives.
                Drives.Clear();
                foreach (var drive in currentDrives)
                {
                    Drives.Add(new DriveViewModel(drive, LoadDirectory));
                }
            }
            catch { }
        }

        private void LoadDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                CurrentPath = path; // Update breadcrumb/path if we had one
                FileList.Clear();
                var dirInfo = new DirectoryInfo(path);

                try
                {
                    // Add supported media files
                    var extensions = new[] { ".wav", ".mp3", ".m4a", ".mov", ".mp4", ".ts", ".mxf", ".avi" };
                    foreach (var file in dirInfo.GetFiles().Where(f => extensions.Contains(f.Extension.ToLower())))
                    {
                        FileList.Add(new MediaItemViewModel(file));
                    }
                }
                catch { /* Access denied or other error */ }
            }
        }

        [RelayCommand]
        private void PreviewFile(MediaItemViewModel item)
        {
             // If clicking the currently playing item, stop it.
             if (SelectedMedia == item && IsPreviewing)
             {
                 StopPreview();
                 return;
             }

             // Stop previous
             if (SelectedMedia != null) SelectedMedia.IsPlaying = false;

             SelectedMedia = item;
             SelectedMedia.IsPlaying = true;
             IsPreviewing = true;
             
             _audioService.Play(item.File.FullName);
        }

        [RelayCommand]
        private void StopPreview()
        {
            _audioService.Stop();
            IsPreviewing = false;
            if (SelectedMedia != null) SelectedMedia.IsPlaying = false;
        }
    }

    public partial class MediaItemViewModel : ObservableObject
    {
        public FileInfo File { get; }
        public string Name => File.Name;
        
        [ObservableProperty]
        private bool _isPlaying;

        public MediaItemViewModel(FileInfo file)
        {
            File = file;
        }
    }

    // Simple ViewModel for TreeView capabilities
    public class DriveViewModel : ObservableObject
    {
        public string Name { get; }
        public string Path { get; }
        public ObservableCollection<FolderViewModel> Folders { get; } = new();
        private readonly Action<string> _onSelect;

        public DriveViewModel(DriveInfo drive, Action<string> onSelect)
        {
            Name = $"{drive.VolumeLabel} ({drive.Name})";
            Path = drive.Name;
            _onSelect = onSelect;
            
            // Lazy loading dummy
            Folders.Add(new FolderViewModel("Loading...", "", null!)); 
            LoadFolders();
        }

        private void LoadFolders()
        {
            Folders.Clear();
            try
            {
                foreach (var dir in new DirectoryInfo(Path).GetDirectories())
                {
                    if ((dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                    {
                         Folders.Add(new FolderViewModel(dir.Name, dir.FullName, _onSelect));
                    }
                }
            }
            catch { }
        }
        
    }

    public class FolderViewModel : ObservableObject
    {
        public string Name { get; }
        public string FullPath { get; }
        public ObservableCollection<FolderViewModel> Children { get; } = new();
        private readonly Action<string> _onSelect;
        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value) && value)
                {
                    LoadChildren();
                }
            }
        }
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value) && value)
                {
                    _onSelect?.Invoke(FullPath);
                }
            }
        }

        public FolderViewModel(string name, string fullPath, Action<string> onSelect)
        {
            Name = name;
            FullPath = fullPath;
            _onSelect = onSelect;

            // Add dummy item for lazy loading if it has children
            // Simplified: just always add dummy to show expansion arrow, verify later
             if (!string.IsNullOrEmpty(fullPath)) 
                 Children.Add(new FolderViewModel("...", "", null!));
        }

        private void LoadChildren()
        {
            // Only load if it contains the dummy
            if (Children.Count == 1 && Children[0].Name == "...")
            {
                Children.Clear();
                try
                {
                    var dirInfo = new DirectoryInfo(FullPath);
                    foreach (var dir in dirInfo.GetDirectories())
                    {
                        if ((dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                        {
                            Children.Add(new FolderViewModel(dir.Name, dir.FullName, _onSelect));
                        }
                    }
                }
                catch { }
            }
        }
    }
}
