using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Veriflow.Avalonia.Models;
using Veriflow.Avalonia.Services;
using Veriflow.Core.Models;
using Avalonia.Platform.Storage;

namespace Veriflow.Avalonia.ViewModels
{
    public enum ReportViewMode
    {
        CameraReport,  // PDF Report view
        EDLLogging     // EDL/Logging view
    }

    public partial class ReportsViewModel : ObservableObject
    {
        // --- DATA ---
        private ReportHeader _videoHeader = new() { ProductionCompany = "Veriflow Video" };
        private ReportHeader _audioHeader = new() { ProductionCompany = "SoundLog Pro Production" };

        [ObservableProperty] private ReportSettings _reportSettings = new();

        [ObservableProperty] private ReportHeader _header;
        
        // Strict Separation
        [ObservableProperty] private ObservableCollection<ReportItem> _videoReportItems = new();
        [ObservableProperty] private ObservableCollection<ReportItem> _audioReportItems = new();

        [ObservableProperty] private ReportViewMode _currentViewMode = ReportViewMode.CameraReport; // Default to Camera Report

        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(ReportTitle))]
        [NotifyPropertyChangedFor(nameof(HasMedia))]
        [NotifyPropertyChangedFor(nameof(HasAnyData))]
        private ReportType _currentReportType = ReportType.Video;

        partial void OnCurrentReportTypeChanged(ReportType value)
        {
            if (value == ReportType.Audio)
            {
                Header = _audioHeader;
            }
            else
            {
                Header = _videoHeader;
            }
            ClearListCommand.NotifyCanExecuteChanged();
            ClearMediaCommand.NotifyCanExecuteChanged();
            PrintCommand.NotifyCanExecuteChanged();
            ExportPdfCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasReport));
        }

        public string ReportTitle => CurrentReportType == ReportType.Audio ? "SOUND REPORT" : "CAMERA REPORT";
        
        public string ClipsCountDisplay => $"{CurrentReportItems.Count} clips";

        [RelayCommand]
        private void AddClip()
        {
             // Logic to add a manual clip or open file picker
             System.Diagnostics.Debug.WriteLine("AddClip Stub");
        }

        public ObservableCollection<ReportItem> CurrentReportItems => CurrentReportType == ReportType.Audio ? AudioReportItems : VideoReportItems;

        // --- STATE ---
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(HasReport))]
        private bool _isReportActive;

        public bool HasReport => IsReportActive && HasMedia;

        [ObservableProperty] private bool _isVideoCalendarOpen;
        [ObservableProperty] private bool _isAudioCalendarOpen;

        /// <summary>
        /// Callback to execute commands with undo/redo support
        /// </summary>
        public System.Action<Commands.IUndoableCommand>? ExecuteCommandCallback { get; set; }

        private readonly IReportPrintingService _printingService;
        private readonly PdfReportService _pdfService;

        public ReportsViewModel()
        {
            _printingService = new ReportPrintingService();
            _pdfService = new PdfReportService();
            _header = _videoHeader; // Initialize default
            SubscribeToHeader();
            
            // Subscriptions to triggers
            VideoReportItems.CollectionChanged += (s, e) => NotifyCollectionChanges();
            AudioReportItems.CollectionChanged += (s, e) => NotifyCollectionChanges();
        }

        private void NotifyCollectionChanges()
        {
            OnPropertyChanged(nameof(HasMedia));
            OnPropertyChanged(nameof(HasReport));
            OnPropertyChanged(nameof(HasAnyData));
             // Explicitly notify commands
            ClearListCommand.NotifyCanExecuteChanged();
            ClearMediaCommand.NotifyCanExecuteChanged();
            PrintCommand.NotifyCanExecuteChanged();
            ExportPdfCommand.NotifyCanExecuteChanged();
            ExportPdfCommand.NotifyCanExecuteChanged();
            ExportSessionEDLCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ClipsCountDisplay));
        }

        public void SetAppMode(AppMode mode)
        {
            CurrentReportType = mode == AppMode.Audio ? ReportType.Audio : ReportType.Video;
        }

        public void AddClipToCurrentReport(ClipLogItem clip)
        {
            // Find the ReportItem corresponding to the source file
            var reportItem = CurrentReportItems.FirstOrDefault(r => 
                r.OriginalMedia.FullName.Equals(clip.SourceFile, System.StringComparison.OrdinalIgnoreCase));
            
            // If not found, auto-create a ReportItem for EDL logging workflow
            if (reportItem == null && !string.IsNullOrEmpty(clip.SourceFile))
            {
                var fileInfo = new System.IO.FileInfo(clip.SourceFile);
                if (fileInfo.Exists)
                {
                    // Create a minimal MediaItemViewModel for the ReportItem
                    var mediaItem = new MediaItemViewModel(fileInfo);
                    reportItem = new ReportItem(mediaItem);
                    CurrentReportItems.Add(reportItem);
                }
            }
            
            if (reportItem != null)
            {
                clip.SourceFile = reportItem.OriginalMedia.FullName; // Ensure full path
                
                // Use Undoable Command
                var command = new Commands.Reports.AddClipCommand(reportItem, clip);
                ExecuteCommandCallback?.Invoke(command);
            }
        }

        [RelayCommand]
        private void RemoveClip(ClipLogItem clip)
        {
            if (clip == null) return;

            // Find parent report item
            var reportItem = CurrentReportItems.FirstOrDefault(r => r.Clips.Contains(clip));
            if (reportItem != null)
            {
                var command = new Commands.Reports.RemoveClipCommand(reportItem, clip);
                ExecuteCommandCallback?.Invoke(command);
            }
        }

        [RelayCommand]
        private void SwitchToReportView()
        {
            CurrentViewMode = ReportViewMode.CameraReport;
        }

        [RelayCommand]
        private void SwitchToEDLView()
        {
            CurrentViewMode = ReportViewMode.EDLLogging;
        }

        [RelayCommand]
        private void OpenTemplatesWindow()
        {
            var vm = new ReportTemplatesViewModel(ReportSettings);
            // Stub template window
            System.Diagnostics.Debug.WriteLine("OpenTemplatesWindow Stub");
        }

        // --- ACTIONS ---

        public void CreateReport(IEnumerable<MediaItemViewModel> items, ReportType type)
        {
            CurrentReportType = type;
            
            // Re-initialize specific header if needed
            if (type == ReportType.Audio) 
            {
                 if (_audioHeader == null) _audioHeader = new ReportHeader() { ProductionCompany = "SoundLog Pro Production" };
                 Header = _audioHeader;
                 AudioReportItems.Clear();
                 foreach (var item in items) AudioReportItems.Add(new ReportItem(item));
            }
            else 
            {
                 if (_videoHeader == null) _videoHeader = new ReportHeader() { ProductionCompany = "Veriflow Video" };
                 Header = _videoHeader;
                 VideoReportItems.Clear();
                 foreach (var item in items) VideoReportItems.Add(new ReportItem(item));
            }

            // AUTO-POPULATE HEADER (New Feature)
            var firstItem = items.FirstOrDefault();
            if (firstItem != null)
            {
                PopulateHeaderFromMedia(firstItem, type);
            }

            IsReportActive = true;
        }

        private void PopulateHeaderFromMedia(MediaItemViewModel item, ReportType type)
        {
            if (type == ReportType.Audio)
            {
                // Map Audio Metadata
                if (item.CurrentMetadata != null)
                {
                    if (!string.IsNullOrWhiteSpace(item.CurrentMetadata.Project)) Header.ProjectName = item.CurrentMetadata.Project;
                    if (!string.IsNullOrWhiteSpace(item.CurrentMetadata.Scene)) Header.Scene = item.CurrentMetadata.Scene;
                    if (!string.IsNullOrWhiteSpace(item.CurrentMetadata.Take)) Header.Take = item.CurrentMetadata.Take;
                    if (!string.IsNullOrWhiteSpace(item.CurrentMetadata.CreationDate) && DateTime.TryParse(item.CurrentMetadata.CreationDate, out DateTime date))
                    {
                         Header.CalendarDate = date;
                    }

                    if (!string.IsNullOrWhiteSpace(item.CurrentMetadata.FrameRate)) 
                        Header.TimecodeRate = item.CurrentMetadata.FrameRate;

                    // Files Type from Extension (e.g. WAV, MP3)
                    string ext = System.IO.Path.GetExtension(item.FullName);
                    if (!string.IsNullOrEmpty(ext)) 
                        Header.FilesType = ext.TrimStart('.').ToUpper();
                }
            }
            else
            {
                // Map Video Metadata (Limited mapping as VideoMetadata usually lacks Production info)
                if (item.CurrentVideoMetadata != null)
                {
                    // Video specific props if available
                    // if (!string.IsNullOrWhiteSpace(item.CurrentVideoMetadata.Project)) Header.ProjectName = ... (Not currently in VideoMetadata)
                }
            }
        }

        public void AddToReport(IEnumerable<MediaItemViewModel> items)
        {
            if (!IsReportActive) return;

            var targetCollection = CurrentReportType == ReportType.Audio ? AudioReportItems : VideoReportItems;
            foreach (var item in items)
            {
                targetCollection.Add(new ReportItem(item));
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task DropFile(DragEventArgs e)
        {
            var files = Veriflow.Avalonia.Services.DragDropHelper.GetFiles(e).ToArray();
            
            if (files != null && files.Length > 0)
            {
                // Auto-switch Audio/Video mode based on file type
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow?.DataContext is MainViewModel mainVM)
                {
                    mainVM.AutoSwitchModeForFiles(files);
                    
                    // Wait for mode switch to complete
                    await System.Threading.Tasks.Task.Delay(100);
                }

                // Create list of MediaItemViewModels from dropped files
                var mediaItems = new List<MediaItemViewModel>();
                foreach (var file in files)
                {
                    if (System.IO.File.Exists(file))
                    {
                        var fileInfo = new System.IO.FileInfo(file);
                        var mediaItem = new MediaItemViewModel(fileInfo);
                        await mediaItem.LoadMetadata();
                        mediaItems.Add(mediaItem);
                    }
                }

                // Create or add to report
                if (mediaItems.Any())
                {
                    if (!IsReportActive)
                    {
                        CreateReport(mediaItems, CurrentReportType);
                    }
                    else
                    {
                        AddToReport(mediaItems);
                    }
                }
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task AddFiles()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                 var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                 if (topLevel?.StorageProvider != null)
                 {
                     var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                     {
                         Title = "Add Files to Report",
                         AllowMultiple = true
                     });

                     if (files != null && files.Count > 0)
                     {
                         var paths = files.Select(x => x.Path.LocalPath).ToArray();
                         
                         // Reuse logic from DropFile but without DragEventArgs wrapper
                         // Refactor if needed, here duplicating slightly for speed
                         var mediaItems = new List<MediaItemViewModel>();
                         foreach (var file in paths)
                         {
                             if (System.IO.File.Exists(file))
                             {
                                 var fileInfo = new System.IO.FileInfo(file);
                                 var mediaItem = new MediaItemViewModel(fileInfo);
                                 await mediaItem.LoadMetadata();
                                 mediaItems.Add(mediaItem);
                             }
                         }

                         if (mediaItems.Any())
                         {
                             if (!IsReportActive) CreateReport(mediaItems, CurrentReportType);
                             else AddToReport(mediaItems);
                         }
                     }
                 }
            }
        }

        [RelayCommand(CanExecute = nameof(HasMedia))]
        private void ClearList()
        {
            var command = new Commands.Reports.ClearListCommand(this, CurrentReportType == ReportType.Video);
            ExecuteCommandCallback?.Invoke(command);
        }

        [RelayCommand(CanExecute = nameof(CanRemoveFile))]
        private void RemoveFile(ReportItem item)
        {
            if (item != null)
            {
                var command = new Commands.Reports.RemoveReportItemCommand(this, item, CurrentReportType == ReportType.Video);
                ExecuteCommandCallback?.Invoke(command);
            }
        }

        private bool CanRemoveFile(ReportItem item) => item != null;

        /// <summary>
        /// Deletes selected items from the current report
        /// </summary>
        public void DeleteSelectedItems()
        {
            if (SelectedReportItem != null)
            {
                var command = new Commands.Reports.RemoveReportItemCommand(this, SelectedReportItem, CurrentReportType == ReportType.Video);
                ExecuteCommandCallback?.Invoke(command);
                    
                SelectedReportItem = null;
            }
        }

        /// <summary>
        /// Checks if there are selected items to delete
        /// </summary>
        public bool HasSelectedItems()
        {
            return SelectedReportItem != null;
        }

        public void NavigateToItem(MediaItemViewModel item)
        {
             var reportItem = CurrentReportItems.FirstOrDefault(r => r.OriginalMedia == item);
             if (reportItem != null)
             {
                 SelectedReportItem = reportItem;
             }
        }

        public ReportItem? GetReportItem(string path)
        {
            // Condition 1: Report Generated (Active)
            if (!IsReportActive) return null;

            // Condition 2: List not empty (Optimization)
            if (!HasMedia) return null;

            // Condition 3: File match (Robust Path Comparison)
            try
            {
                var fullPath = System.IO.Path.GetFullPath(path);
                return CurrentReportItems.FirstOrDefault(r => 
                    string.Equals(System.IO.Path.GetFullPath(r.OriginalMedia.FullName), fullPath, System.StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // Fallback to simple comparison if path is invalid
                return CurrentReportItems.FirstOrDefault(r => r.OriginalMedia.FullName.Equals(path, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        public void NavigateToPath(string path)
        {
             var reportItem = GetReportItem(path);
             if (reportItem != null)
             {
                 SelectedReportItem = reportItem;
             }
        }

        public bool HasMedia
        {
            get
            {
                return CurrentReportType == ReportType.Audio ? AudioReportItems.Count > 0 : VideoReportItems.Count > 0;
            }
        }
        
        public bool HasInfos
        {
            get
            {
                if (Header == null) return false;
                bool hasData = !string.IsNullOrEmpty(Header.ReportDate) ||
                               !string.IsNullOrEmpty(Header.ProjectName) || 
                               !string.IsNullOrEmpty(Header.OperatorName) || 
                               !string.IsNullOrEmpty(Header.Director) ||
                               !string.IsNullOrEmpty(Header.Dop) ||
                               !string.IsNullOrEmpty(Header.SoundMixer) ||
                               !string.IsNullOrEmpty(Header.BoomOperator) ||
                               !string.IsNullOrEmpty(Header.Location) ||
                               !string.IsNullOrEmpty(Header.TimecodeRate) ||
                               !string.IsNullOrEmpty(Header.FilesType) ||
                               !string.IsNullOrEmpty(Header.DataManager) ||
                               !string.IsNullOrEmpty(Header.CameraId) ||
                               !string.IsNullOrEmpty(Header.ReelName) ||
                               !string.IsNullOrEmpty(Header.LensInfo) ||
                               !string.IsNullOrEmpty(Header.Episode) ||
                               !string.IsNullOrEmpty(Header.Scene) ||
                               !string.IsNullOrEmpty(Header.Take) ||
                               !string.IsNullOrEmpty(Header.GlobalNotes);
                return hasData;
            }
        }
        public bool HasAnyData => HasMedia || HasInfos;


        [RelayCommand(CanExecute = nameof(HasMedia))]
        private void ClearMedia()
        {
            ClearList(); // Logic reused
        }

        [RelayCommand(CanExecute = nameof(HasInfos))]
        private void ClearInfos()
        {
            var command = new Commands.Reports.ClearInfosCommand(this);
            ExecuteCommandCallback?.Invoke(command);
        }

        [RelayCommand(CanExecute = nameof(HasAnyData))]
        public void ClearAll()
        {
            ClearMedia();
            ClearInfos(); 
        }
        
        // Hook into Header changes
        partial void OnHeaderChanged(ReportHeader value)
        {
            SubscribeToHeader();
            OnPropertyChanged(nameof(HasInfos));
            OnPropertyChanged(nameof(HasAnyData));
            ClearInfosCommand.NotifyCanExecuteChanged();
            ClearAllCommand.NotifyCanExecuteChanged();
        }

        private void SubscribeToHeader()
        {
            if (Header != null)
            {
                Header.PropertyChanged -= Header_PropertyChanged;
                Header.PropertyChanged += Header_PropertyChanged;
            }
        }

        private void Header_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
             if (e.PropertyName == nameof(ReportHeader.CalendarDate))
             {
                 if (CurrentReportType == ReportType.Audio) IsAudioCalendarOpen = false;
                 else IsVideoCalendarOpen = false;
             }
 
             OnPropertyChanged(nameof(HasInfos));
             OnPropertyChanged(nameof(HasAnyData));
             ClearInfosCommand.NotifyCanExecuteChanged();
             ClearAllCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty] private ReportItem? _selectedReportItem;


        // --- COMMANDS ---
        
        [RelayCommand(CanExecute = nameof(HasReport))]
        private void Print()
        {
            if (!IsReportActive) return;
            // _printingService.PrintReport(Header, CurrentReportItems, CurrentReportType);
            System.Diagnostics.Debug.WriteLine("Print Stub");
        }

        [RelayCommand(CanExecute = nameof(HasMedia))]
        private async System.Threading.Tasks.Task ExportPdf()
        {
             if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
             {
                 var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                 if (topLevel?.StorageProvider != null)
                 {
                     var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                     {
                         Title = "Export PDF Report",
                         DefaultExtension = ".pdf",
                         SuggestedFileName = CurrentReportType == ReportType.Video ? "camera_report" : "sound_report",
                         FileTypeChoices = new[] { new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } } }
                     });

                     if (file != null)
                     {
                         try
                         {
                             bool isVideo = CurrentReportType == ReportType.Video;
                             _pdfService.GeneratePdf(file.Path.LocalPath, Header, CurrentReportItems, isVideo, ReportSettings);
                             
                             // Optional: MessageBox success
                             // System.Diagnostics.Debug.WriteLine($"PDF Saved to {file.Path.LocalPath}");
                         }
                         catch (Exception ex)
                         {
                             System.Diagnostics.Debug.WriteLine($"PDF Export Error: {ex.Message}");
                         }
                     }
                 }
             }
        }

        [RelayCommand(CanExecute = nameof(CanExportSessionEDL))]
        private async System.Threading.Tasks.Task ExportSessionEDL()
        {
            // Collect all clips
            var allClips = CurrentReportItems
                .Where(item => item.Clips.Any())
                .SelectMany(item => item.Clips)
                .ToList();

            if (allClips.Count == 0) return;

             if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
             {
                 var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                 if (topLevel?.StorageProvider != null)
                 {
                     var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                     {
                         Title = "Export Session EDL/ALE",
                         DefaultExtension = ".edl",
                         SuggestedFileName = $"Session_{DateTime.Now:yyyyMMdd_HHmmss}",
                         FileTypeChoices = new[] { new FilePickerFileType("Edit Decision List") { Patterns = new[] { "*.edl" } } }
                     });

                     if (file != null)
                     {
                         try
                         {
                             string edlPath = file.Path.LocalPath;
                             string alePath = System.IO.Path.ChangeExtension(edlPath, ".ale");

                             var edlContent = GenerateSessionEdl(allClips);
                             await System.IO.File.WriteAllTextAsync(edlPath, edlContent);

                             var aleContent = GenerateSessionAle(allClips);
                             await System.IO.File.WriteAllTextAsync(alePath, aleContent);
                         }
                         catch (Exception ex)
                         {
                             System.Diagnostics.Debug.WriteLine($"Export Failed: {ex.Message}");
                         }
                     }
                 }
             }
        }

        private bool CanExportSessionEDL()
        {
            return CurrentReportItems.Any(item => item.Clips.Any());
        }

        private string GenerateSessionEdl(List<ClipLogItem> clips)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("TITLE: VERIFLOW_SESSION_EXPORT");
            sb.AppendLine("FCM: NON-DROP FRAME");
            sb.AppendLine();

            TimeSpan destTime = new TimeSpan(1, 0, 0);
            int index = 1;

            foreach (var clip in clips)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(clip.SourceFile);
                var reelName = fileName.Length > 8 ? fileName.Substring(0, 8) : fileName.PadRight(8);

                // Parse times (format: hh:mm:ss:ff)
                string srcIn = clip.InPoint;
                string srcOut = clip.OutPoint;
                string dstIn = $"{destTime.Hours:D2}:{destTime.Minutes:D2}:{destTime.Seconds:D2}:00";
                
                // Calculate duration
                // var duration = TimeSpan.Parse(clip.Duration); // Be careful with parsing custom format
                // destTime += duration;
                 string dstOut = dstIn; // Stub duration update

                sb.AppendLine($"{index:D3}  {reelName,-8} V     C        {srcIn} {srcOut} {dstIn} {dstOut}");
                sb.AppendLine($"* FROM CLIP: {fileName}");
                if (!string.IsNullOrEmpty(clip.Notes))
                    sb.AppendLine($"* NOTES: {clip.Notes}");
                sb.AppendLine();

                index++;
            }

            return sb.ToString();
        }

        private string GenerateSessionAle(List<ClipLogItem> clips)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Heading");
            sb.AppendLine("FIELD_DELIM:\tTABS");
            sb.AppendLine("VIDEO_FORMAT:\t1080");
            sb.AppendLine("FPS:\t25.00");
            sb.AppendLine();
            sb.AppendLine("Name\tSource\tStart\tEnd\tDuration\tNotes");
            sb.AppendLine("Data");

            foreach (var clip in clips)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(clip.SourceFile);
                sb.AppendLine($"{clip.Notes}\t{fileName}\t{clip.InPoint}\t{clip.OutPoint}\t{clip.Duration}\t{clip.Notes}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clears all reports for session management
        /// </summary>
        public void ClearAllReports()
        {
            AudioReportItems.Clear();
            VideoReportItems.Clear();
            IsReportActive = false;
        }

        /// <summary>
        /// Restores a report item from session data
        /// </summary>
        public void RestoreReportItem(Veriflow.Core.Models.ReportItemData reportData, bool isVideo)
        {
            try
            {
                if (!System.IO.File.Exists(reportData.FilePath)) return;

                var fileInfo = new System.IO.FileInfo(reportData.FilePath);
                var mediaItem = new MediaItemViewModel(fileInfo);
                
                var reportItem = new ReportItem(mediaItem)
                {
                    Scene = reportData.Scene,
                    Take = reportData.Take,
                    ItemNotes = reportData.Notes
                };

                // Restore clips if any
                if (reportData.Clips != null)
                {
                    foreach (var clipData in reportData.Clips)
                    {
                        reportItem.Clips.Add(new ClipLogItem
                        {
                            SourceFile = reportData.FilePath,
                            InPoint = clipData.InPoint,
                            OutPoint = clipData.OutPoint,
                            Notes = clipData.Notes
                        });
                    }
                }

                if (isVideo)
                {
                    VideoReportItems.Add(reportItem);
                }
                else
                {
                    AudioReportItems.Add(reportItem);
                }

                IsReportActive = true;
            }
            catch
            {
                // Skip items that can't be restored
            }
        }
    }
}
