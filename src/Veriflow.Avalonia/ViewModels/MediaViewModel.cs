using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Veriflow.Avalonia.Services;
using Veriflow.Avalonia.Models;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Veriflow.Avalonia.ViewModels
{
    public enum MediaViewMode
    {
        Grid,      // Icon view with thumbnails
        List,      // Detailed table view
        Filmstrip  // List + large preview
    }

    public partial class MediaViewModel : ObservableObject
    {
        public ObservableCollection<MediaItemViewModel> MediaFiles { get; } = new();
        private readonly AudioPreviewService _audioService = new();
        private readonly MetadataEditorService _metadataEditorService = new();

        [ObservableProperty]
        private ObservableCollection<DriveViewModel> _drives = new();

        [ObservableProperty]
        private FileExplorerViewModel _fileExplorer = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenInPlayerCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendFileToTranscodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditMetadataCommand))]
        private MediaItemViewModel? _selectedMedia;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReportCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddToReportCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendToOffloadCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendToTranscodeCommand))]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyMessage = "";

        partial void OnSelectedMediaChanged(MediaItemViewModel? value)
        {
            value?.LoadMetadata(); 
            
            if (CurrentViewMode == MediaViewMode.Filmstrip && IsVideoPlaying)
            {
                _ = StopFilmstrip(); 
            }
        }

        [RelayCommand]
        private void SelectMedia(MediaItemViewModel item)
        {
            if (SelectedMedia != null) 
                SelectedMedia.IsPlaying = false;
            
            SelectedMedia = item;
            // Metadata loads automatically via OnSelectedMediaChanged
        }

        [ObservableProperty]
        private bool _isPreviewing;
        
        [ObservableProperty]
        private bool _isStopPressed;
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendToOffloadCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendToTranscodeCommand))]
        private string? _currentPath = null;

        private string? _lastVideoPath = null;
        private string? _lastAudioPath = null;

        public event Action<string>? RequestOffloadSource;
        public event Action<IEnumerable<string>>? RequestTranscode;

        [ObservableProperty]
        private bool _isVideoMode;

        [ObservableProperty]
        private MediaViewMode _currentViewMode = MediaViewMode.Grid; 

        partial void OnCurrentViewModeChanged(MediaViewMode value)
        {
            StopPreview();
            
            // Notify icon property changes
            OnPropertyChanged(nameof(GridIconBrush));
            OnPropertyChanged(nameof(ListIconBrush));
            OnPropertyChanged(nameof(FilmstripIconBrush));
            OnPropertyChanged(nameof(GridIconOpacity));
            OnPropertyChanged(nameof(ListIconOpacity));
            OnPropertyChanged(nameof(FilmstripIconOpacity));
        }

        [RelayCommand]
        private void SetViewMode(string mode)
        {
            CurrentViewMode = mode switch
            {
                "Grid" => MediaViewMode.Grid,
                "List" => MediaViewMode.List,
                "Filmstrip" => MediaViewMode.Filmstrip,
                _ => CurrentViewMode
            };
        }

        // Icon color properties for view mode buttons
        public IBrush GridIconBrush
        {
            get
            {
                if (CurrentViewMode == MediaViewMode.Grid && 
                    global::Avalonia.Application.Current?.TryGetResource("Brush.Accent", null, out var resource) == true &&
                    resource is IBrush brush)
                {
                    return brush;
                }
                return Brushes.White;
            }
        }

        public IBrush ListIconBrush
        {
            get
            {
                if (CurrentViewMode == MediaViewMode.List && 
                    global::Avalonia.Application.Current?.TryGetResource("Brush.Accent", null, out var resource) == true &&
                    resource is IBrush brush)
                {
                    return brush;
                }
                return Brushes.White;
            }
        }

        public IBrush FilmstripIconBrush
        {
            get
            {
                if (CurrentViewMode == MediaViewMode.Filmstrip && 
                    global::Avalonia.Application.Current?.TryGetResource("Brush.Accent", null, out var resource) == true &&
                    resource is IBrush brush)
                {
                    return brush;
                }
                return Brushes.White;
            }
        }

        public double GridIconOpacity => CurrentViewMode == MediaViewMode.Grid ? 1.0 : 0.4;
        public double ListIconOpacity => CurrentViewMode == MediaViewMode.List ? 1.0 : 0.4;
        public double FilmstripIconOpacity => CurrentViewMode == MediaViewMode.Filmstrip ? 1.0 : 0.4;

        public void SetAppMode(AppMode mode)
        {
            FileList.Clear();

            if (IsVideoMode)
                _lastVideoPath = CurrentPath;
            else
                _lastAudioPath = CurrentPath;

            IsVideoMode = (mode == AppMode.Video);

            if (IsVideoMode)
            {
                InitializePreviewPlayer();
            }

            string? targetPath = IsVideoMode ? _lastVideoPath : _lastAudioPath;
            
            CurrentPath = targetPath;
            
            if (!string.IsNullOrEmpty(CurrentPath))
            {
                LoadDirectory(CurrentPath);
                _ = ExpandAndSelectPath(CurrentPath);
            }
        }


        public event Action<IEnumerable<MediaItemViewModel>, bool>? RequestCreateReport; // true=Video
        public event Action<IEnumerable<MediaItemViewModel>>? RequestAddToReport;

        private bool _hasVideoReport = false;
        private bool _hasAudioReport = false;

        private HashSet<string> _currentReportFilePaths = new();

        public void UpdateReportContext(IEnumerable<string> reportPaths, bool isReportActive)
        {
            _currentReportFilePaths = isReportActive 
                ? new HashSet<string>(reportPaths, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            if (IsVideoMode) _hasVideoReport = isReportActive;
            else _hasAudioReport = isReportActive;

            AddToReportCommand.NotifyCanExecuteChanged();
        }

        public void SetReportStatus(bool isVideo, bool hasContent)
        {
            if (isVideo) _hasVideoReport = hasContent;
            else _hasAudioReport = hasContent;

            AddToReportCommand.NotifyCanExecuteChanged();
        }

        private bool CanSendToOffload() => !string.IsNullOrWhiteSpace(CurrentPath) && Directory.Exists(CurrentPath) && !IsBusy;
        private bool CanSendToTranscode() => !string.IsNullOrWhiteSpace(CurrentPath) && Directory.Exists(CurrentPath) && FileList.Any() && !IsBusy;

        private bool CanCreateReport() => FileList.Any() && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanCreateReport))]
        private async Task CreateReport()
        {
            if (FileList.Any())
            {
               await Task.WhenAll(FileList.Select(item => item.LoadMetadata()));
               
               RequestCreateReport?.Invoke(FileList.ToList(), IsVideoMode);
            }
        }

        private bool CanAddToReport() 
        {
            bool isReportActive = IsVideoMode ? _hasVideoReport : _hasAudioReport;
            if (!isReportActive) return false;
            
            bool hasNewFiles = FileList.Any(file => !_currentReportFilePaths.Contains(file.FullName));
            
            return hasNewFiles;
        }

        [RelayCommand(CanExecute = nameof(CanAddToReport))]
        private async Task AddToReport()
        {
             if (FileList.Any())
            {
               await Task.WhenAll(FileList.Select(item => item.LoadMetadata()));

               RequestAddToReport?.Invoke(FileList.ToList());
            }
        }

        [RelayCommand(CanExecute = nameof(CanSendToOffload))]
        private void SendToOffload()
        {
             RequestOffloadSource?.Invoke(CurrentPath ?? "");
        }

        private bool CanSendFileToTranscode() => SelectedMedia != null;

        [RelayCommand(CanExecute = nameof(CanSendFileToTranscode))]
        private void SendFileToTranscode()
        {
            if (SelectedMedia != null)
                RequestTranscode?.Invoke(new List<string> { SelectedMedia.FullName });
        }

        [RelayCommand(CanExecute = nameof(CanSendToTranscode))]
        private void SendToTranscode()
        {
            var files = FileList.Select(x => x.FullName).ToList();
            if (files.Any())
                RequestTranscode?.Invoke(files);
        }

        private bool CanEditMetadata() => SelectedMedia != null;

        [RelayCommand(CanExecute = nameof(CanEditMetadata))]
        private async Task EditMetadata()
        {
            if (SelectedMedia == null) return;

            // STUB: Warning and MetadataEditWindow
            await Task.Delay(1);
        }

        [ObservableProperty]
        private ObservableCollection<MediaItemViewModel> _fileList = new();

        private System.Timers.Timer _driveWatcher;

        public MediaViewModel()
        {
            RefreshDrives();

            _driveWatcher = new System.Timers.Timer(3000); 
            _driveWatcher.Elapsed += (s, e) => 
            {
                try 
                {
                    Dispatcher.UIThread.InvokeAsync(() => 
                    {
                         RefreshDrives();
                    });
                }
                catch {}
            };
            _driveWatcher.Start();

            FileList.CollectionChanged += (s, e) => 
            {
                SendToTranscodeCommand.NotifyCanExecuteChanged();
                CreateReportCommand.NotifyCanExecuteChanged();
            };

            InitializePreviewPlayer();
            
            // Connect FileExplorer selection to LoadDirectory
            FileExplorer.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FileExplorerViewModel.SelectedDirectory))
                {
                    if (!string.IsNullOrEmpty(FileExplorer.SelectedDirectory))
                    {
                        LoadDirectory(FileExplorer.SelectedDirectory);
                    }
                }
            };
        }

        private void RefreshDrives()
        {
            var safeDriveList = new List<DriveViewModel>();

            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    try 
                    {
                        if (drive.IsReady)
                        {
                            safeDriveList.Add(new DriveViewModel(drive, LoadDirectory));
                        }
                    }
                    catch {}
                }
            }
            catch {}

            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateDrivesCollection(safeDriveList);
            }
            else
            {
                Dispatcher.UIThread.Invoke(() => UpdateDrivesCollection(safeDriveList));
            }
        }

        private void UpdateDrivesCollection(List<DriveViewModel> newDrives)
        {
            Drives.Clear();
            foreach (var d in newDrives)
            {
                Drives.Add(d);
            }
        }

        public static readonly string[] AudioExtensions = { ".wav", ".mp3", ".m4a", ".aac", ".aiff", ".aif", ".flac", ".ogg", ".opus", ".ac3" };
        public static readonly string[] VideoExtensions = { ".mov", ".mp4", ".ts", ".mxf", ".avi", ".mkv", ".webm", ".wmv", ".flv", ".m4v", ".mpg", ".mpeg", ".3gp", ".dv", ".ogv", ".m2v", ".vob", ".m2ts" };

        private void LoadDirectory(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            if (Directory.Exists(path))
            {
                CurrentPath = path; 
                FileList.Clear();
                var dirInfo = new DirectoryInfo(path);

                try
                {
                    IsBusy = true;
                    BusyMessage = "Loading files...";

                    var targetExtensions = IsVideoMode ? VideoExtensions : AudioExtensions;
                    
                    var files = dirInfo.GetFiles()
                                       .Where(f => targetExtensions.Contains(f.Extension.ToLower()))
                                       .OrderBy(f => f.Name);

                    foreach (var file in files)
                    {
                        FileList.Add(new MediaItemViewModel(file));
                    }

                    BusyMessage = "Loading metadata...";
                }
                catch 
                { 
                    IsBusy = false;
                    BusyMessage = "";
                }

                AddToReportCommand.NotifyCanExecuteChanged();

                _ = PreloadMetadataAsync();
            }
        }

        private async Task PreloadMetadataAsync()
        {
            try
            {
                var tasks = FileList.Select(item => item.LoadMetadata()).ToList();
                await Task.WhenAll(tasks);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = "";
            }
        }

        partial void OnIsVideoModeChanged(bool value)
        {
            StopPreview();
            AddToReportCommand.NotifyCanExecuteChanged();
            
            // Notify icon color changes when profile switches (Video/Audio)
            OnPropertyChanged(nameof(GridIconBrush));
            OnPropertyChanged(nameof(ListIconBrush));
            OnPropertyChanged(nameof(FilmstripIconBrush));
        }

        [ObservableProperty]
        private bool _isVideoPlaying;

        public MediaPlayer? PreviewPlayer { get; private set; }
        private Views.VideoPreviewWindow? _previewWindow;

        private void InitializePreviewPlayer()
        {
            if (PreviewPlayer == null)
            {
                VideoEngineService.Instance.Initialize();
                if (VideoEngineService.Instance.LibVLC != null)
                {
                    PreviewPlayer = new MediaPlayer(VideoEngineService.Instance.LibVLC);
                }
            }
        }

        [RelayCommand]
        private async Task PreviewFile(MediaItemViewModel item)
        {
             if (SelectedMedia == item && (IsPreviewing || IsVideoPlaying))
             {
                 StopPreview();
                 return;
             }

             if (SelectedMedia != null) SelectedMedia.IsPlaying = false;

             SelectedMedia = item;
             SelectedMedia.IsPlaying = true;

             if (IsVideoMode)
             {
                 InitializePreviewPlayer();
                 if (PreviewPlayer != null && VideoEngineService.Instance.LibVLC != null)
                 {
                     IsVideoPlaying = true;
                     using var media = new Media(VideoEngineService.Instance.LibVLC, item.FullName, FromType.FromPath);
                     media.AddOption(":avcodec-hw=d3d11va"); 
                     
                     // Create and show chromeless preview window
                     _previewWindow = new Views.VideoPreviewWindow();
                     _previewWindow.SetPlayer(PreviewPlayer);
                     _previewWindow.SetTitle(item.Name);
                     _previewWindow.Closed += (s, e) => 
                     {
                         StopPreview();
                         _previewWindow = null;
                     };
                     _previewWindow.Show();
                     
                     // CRITICAL: Wait for VideoView to initialize before playing
                     await _previewWindow.InitializeAsync();
                     PreviewPlayer.Play(media);
                 }
             }
             else
             {
                 IsPreviewing = true;
                 _audioService.Play(item.File.FullName);
             }
        }

        [RelayCommand]
        private void StopPreview()
        {
            if (IsVideoPlaying)
            {
                PreviewPlayer?.Stop();
                _previewWindow?.Close();
                _previewWindow = null;
                IsVideoPlaying = false;
            }
            
            if (IsPreviewing)
            {
                _audioService.Stop();
                IsPreviewing = false;
            }
            
            if (SelectedMedia != null)
            {
                SelectedMedia.IsPlaying = false;
            }
        }

        [RelayCommand]
        private void PlayFilmstrip()
        {
            if (SelectedMedia == null || CurrentViewMode != MediaViewMode.Filmstrip)
                return;

            try
            {
                if (IsVideoMode)
                {
                    InitializePreviewPlayer();
                    if (PreviewPlayer != null && VideoEngineService.Instance.LibVLC != null)
                    {
                        IsVideoPlaying = true;
                        SelectedMedia.IsPlaying = true;
                        using var media = new Media(VideoEngineService.Instance.LibVLC, SelectedMedia.FullName, FromType.FromPath);
                        media.AddOption(":avcodec-hw=d3d11va");
                        PreviewPlayer.Play(media);
                    }
                }
                else
                {
                    _audioService.Play(SelectedMedia.FullName);
                    IsVideoPlaying = true; 
                    SelectedMedia.IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to play in filmstrip: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task StopFilmstrip()
        {
            try
            {
                IsStopPressed = true;
                
                PreviewPlayer?.Stop();
                _audioService.Stop();
                
                IsVideoPlaying = false;
                if (SelectedMedia != null) SelectedMedia.IsPlaying = false;
                
                await Task.Delay(200);
                IsStopPressed = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to stop filmstrip: {ex.Message}");
                IsStopPressed = false;
            }
        }

        [RelayCommand]
        private void ToggleFilmstripPlayback()
        {
            if (IsVideoPlaying)
            {
                _ = StopFilmstrip(); 
            }
            else
            {
                PlayFilmstrip();
            }
        }

        private bool CanUnloadMedia() => SelectedMedia != null;

        [RelayCommand(CanExecute = nameof(CanUnloadMedia))]
        private void UnloadMedia()
        {
            StopPreview();
            SelectedMedia = null;
        }

        public event Action<string>? RequestOpenInPlayer;

        private bool CanOpenInPlayer() => SelectedMedia != null;

        [RelayCommand(CanExecute = nameof(CanOpenInPlayer))]
        private void OpenInPlayer()
        {
            if (SelectedMedia != null)
            {
                StopPreview();
                RequestOpenInPlayer?.Invoke(SelectedMedia.FullName);
            }
        }

        public void RemoveSelectedFiles()
        {
            if (SelectedMedia != null)
            {
                var itemToRemove = SelectedMedia;
                SelectedMedia = null;
                FileList.Remove(itemToRemove);
            }
        }

        public bool HasSelectedFiles()
        {
            return SelectedMedia != null;
        }

        [RelayCommand]
        private Task DropMedia(DragEventArgs e) => HandleDrop(e);

        [RelayCommand]
        private Task DropExplorer(DragEventArgs e) => HandleDrop(e);

        private async Task HandleDrop(DragEventArgs e)
        {
            var files = DragDropHelper.GetFiles(e).ToArray();
            if (files.Length > 0)
            {
                 // Check if first item is directory
                 if (Directory.Exists(files[0]))
                 {
                     LoadDirectory(files[0]);
                     await ExpandAndSelectPath(files[0]);
                 }
                 else
                 {
                     // Dropped files? MediaView is explorer-based, so we load the parent directory of the first file
                     var dir = Path.GetDirectoryName(files[0]);
                     if (!string.IsNullOrEmpty(dir))
                     {
                         LoadDirectory(dir);
                         await ExpandAndSelectPath(dir);
                         
                         // Select the file
                         var fileVM = FileList.FirstOrDefault(f => f.FullName == files[0]);
                         if (fileVM != null) SelectedMedia = fileVM;
                     }
                 }
            }
        }

        private async Task<bool> ExpandAndSelectPath(string path)
        {
             var drive = Drives.FirstOrDefault(d => path.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));
             if (drive == null) return false;

             drive.IsExpanded = true;
             await Task.Delay(100); 

             string currentPath = drive.Path;
             if (!currentPath.EndsWith(Path.DirectorySeparatorChar.ToString())) 
                 currentPath += Path.DirectorySeparatorChar;

             var targetParts = path.Substring(drive.Path.Length).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
             
             ObservableCollection<FolderViewModel> currentCollection = drive.Folders;
             object currentItem = drive;

              foreach (var part in targetParts)
              {
                  var folder = currentCollection.FirstOrDefault(f => f.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                  if (folder == null) return false; 

                  folder.IsExpanded = true;
                  await Task.Delay(50); 
                  
                  currentCollection = folder.Folders;
                  currentItem = folder;
              }
              
             if (currentItem is FolderViewModel finalFolder)
             {
                 if (!finalFolder.IsSelected)
                 {
                      finalFolder.IsSelected = true;
                 }
             }
             else if (currentItem is DriveViewModel finalDrive)
             {
                 if (!finalDrive.IsSelected)
                 {
                      finalDrive.IsSelected = true;
                 }
             }
             
             return true; 
        }

        public List<string> GetLoadedFiles()
        {
            return FileList.Select(f => f.FullName).ToList();
        }

        public void LoadFiles(List<string> filePaths)
        {
            if (filePaths == null || !filePaths.Any()) return;

            var firstFile = filePaths.FirstOrDefault();
            if (!string.IsNullOrEmpty(firstFile) && File.Exists(firstFile))
            {
                var directory = Path.GetDirectoryName(firstFile);
                if (!string.IsNullOrEmpty(directory))
                {
                    LoadDirectory(directory);
                }
            }
        }

        public void ClearAllFiles()
        {
            MediaFiles.Clear();
        }
    }

    public partial class MediaItemViewModel : ObservableObject
    {
        public FileInfo File { get; }
        public string Name => File.Name;
        public string FullName => File.FullName;
        public DateTime CreationTime => File.CreationTime;
        public long Length => File.Length;
        public string FileSizeFormatted => $"{(File.Length / 1024.0 / 1024.0):F2} MB";

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private AudioMetadata _currentMetadata = new();

        [ObservableProperty]
        private VideoMetadata _currentVideoMetadata = new();
        
        [ObservableProperty] private string _duration = "--:--";
        [ObservableProperty] private string _sampleRate = "";
        [ObservableProperty] private string _channels = "";
        [ObservableProperty] private string _bitDepth = "";
        [ObservableProperty] private string _format = "";
        
        [ObservableProperty] private string? _thumbnailPath;
        
        // Avalonia requires Bitmap object for Image binding (string path doesn't auto-convert like WPF)
        [ObservableProperty] private global::Avalonia.Media.Imaging.Bitmap? _thumbnailBitmap;

        private bool _metadataLoaded;

        public MediaItemViewModel(FileInfo file)
        {
            File = file;

            string ext = File.Extension.ToLower();
            if (MediaViewModel.VideoExtensions.Contains(ext))
            {
                TriggerThumbnailLoad();
            }
        }

        private void TriggerThumbnailLoad()
        {
             _ = Task.Run(async () => 
            {
                try
                {
                    var thumbService = new ThumbnailService(); 
                    string? thumbPath = await thumbService.GetThumbnailAsync(File.FullName);
                    
                    if (!string.IsNullOrEmpty(thumbPath) && System.IO.File.Exists(thumbPath))
                    {
                        // Load Bitmap on background thread to avoid UI blocking
                        var bitmap = await Task.Run(() => new global::Avalonia.Media.Imaging.Bitmap(thumbPath));
                        
                        // Update UI on UI thread
                        await Dispatcher.UIThread.InvokeAsync(() => 
                        {
                            ThumbnailPath = thumbPath; // Keep path for reference
                            ThumbnailBitmap = bitmap;  // Avalonia needs Bitmap object
                        });
                        
                        Debug.WriteLine($"[MediaItemViewModel] Thumbnail loaded: {thumbPath}");
                    }
                    else
                    {
                        Debug.WriteLine($"[MediaItemViewModel] Thumbnail generation failed for: {File.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MediaItemViewModel] Thumbnail error: {ex.Message}");
                }
            });
        }

        public async Task LoadMetadata()
        {
            if (_metadataLoaded) return;
            
            try
            {
                var provider = new FFprobeMetadataProvider();
                string ext = File.Extension.ToLower();
                bool isVideo = MediaViewModel.VideoExtensions.Contains(ext);

                if (isVideo)
                {
                    CurrentVideoMetadata = await provider.GetVideoMetadataAsync(File.FullName);
                    if (CurrentVideoMetadata != null)
                    {
                        Duration = CurrentVideoMetadata.Duration;
                        Format = CurrentVideoMetadata.Resolution; 
                    }
                }
                else
                {
                    CurrentMetadata = await provider.GetMetadataAsync(File.FullName);
                    
                    if (CurrentMetadata != null)
                    {
                        Duration = CurrentMetadata.Duration;
                        Format = CurrentMetadata.Format;
                        
                        var formatParts = CurrentMetadata.Format.Split('/');
                        SampleRate = !string.IsNullOrEmpty(CurrentMetadata.SampleRateString) 
                                     ? CurrentMetadata.SampleRateString 
                                     : (formatParts.Length > 0 ? formatParts[0].Trim() : "");

                        BitDepth = !string.IsNullOrEmpty(CurrentMetadata.BitDepthString) 
                                   ? CurrentMetadata.BitDepthString 
                                   : (formatParts.Length > 1 ? formatParts[1].Trim() : "");
                        
                        Channels = CurrentMetadata.ChannelCount.ToString(); 
                        if (CurrentMetadata.ChannelCount == 1) Channels = "Mono";
                        else if (CurrentMetadata.ChannelCount == 2) Channels = "Stereo";
                        else Channels = $"{CurrentMetadata.ChannelCount} Ch";
                    }
                }

                _metadataLoaded = true;
            }
            catch (Exception)
            {
                Format = "Unknown";
                CurrentMetadata = new AudioMetadata { Filename = File.Name };
                CurrentVideoMetadata = new VideoMetadata { Filename = File.Name };
            }
        }


    }
}
