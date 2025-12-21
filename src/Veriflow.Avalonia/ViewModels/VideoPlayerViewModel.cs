using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Input;
using Veriflow.Avalonia.Models;
using Veriflow.Avalonia.Services;
using System.Linq;
using System.IO;
using System.Text;


namespace Veriflow.Avalonia.ViewModels
{
    public partial class VideoPlayerViewModel : ObservableObject, IDisposable
    {
        private MediaPlayer? _mediaPlayer;

        public bool ShowVolumeControls => true; // Video Player uses internal volume slider

        // Unified Volume Control
        private float _volume = 1.0f;
        private float _preMuteVolume = 1.0f; // Store volume before muting

        private double _fps = 25.0; // Default Frame Rate
        private long _lastMediaTime;
        private System.Diagnostics.Stopwatch _stopwatch = new();
        private TimeSpan _startHeaderOffset = TimeSpan.Zero;

        public float Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    if (_mediaPlayer != null) _mediaPlayer.Volume = (int)(value * 100);

                    // "Unmute on Drag" feature
                    if (value > 0 && IsMuted)
                    {
                        IsMuted = false;
                    }
                }
            }
        }

        private bool _isMuted;
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (SetProperty(ref _isMuted, value))
                {
                    if (_mediaPlayer != null) _mediaPlayer.Mute = value;

                    if (value) // MUTE ACTIVATION
                    {
                        if (Volume > 0) _preMuteVolume = Volume;
                        Volume = 0;
                    }
                    else // MUTE DEACTIVATION
                    {
                        if (Volume == 0)
                        {
                            Volume = _preMuteVolume > 0.05f ? _preMuteVolume : 0.5f; 
                        }
                    }
                }
            }
        }

        [RelayCommand]
        private void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        [ObservableProperty]
        private MediaPlayer? _player; 

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMedia))]
        private string _filePath = "No file loaded";

        public bool HasMedia => !string.IsNullOrEmpty(FilePath) && FilePath != "No file loaded";

        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        [NotifyCanExecuteChangedFor(nameof(TogglePlayPauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(SendFileToTranscodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(UnloadMediaCommand))]
        private bool _isVideoLoaded;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(TogglePlayPauseCommand))]
        private bool _isPlaying;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(TogglePlayPauseCommand))]
        private bool _isPaused;

        [ObservableProperty]
        private string _currentTimeDisplay = "00:00:00:00";

        [ObservableProperty]
        private string _totalTimeDisplay = "00:00:00:00";

        [ObservableProperty]
        private double _playbackPercent;

        [ObservableProperty]
        private VideoMetadata _currentVideoMetadata = new();

        // --- PROFESSIONAL LOGGING ---
        public ObservableCollection<ClipLogItem> TaggedClips { get; } = new();

        [ObservableProperty]
        private ClipLogItem? _editingClip;

        private ClipLogItem? _originalClipData;

        private TimeSpan? _currentInPoint;
        private TimeSpan? _currentOutPoint;

        [ObservableProperty]
        private string _currentMarkInDisplay = "xx:xx:xx:xx";

        [ObservableProperty]
        private string _currentMarkOutDisplay = "yy:yy:yy:yy";

        [ObservableProperty]
        private bool _isLoggingPending;

        public event Action<IEnumerable<string>>? RequestTranscode;

        // Callbacks / Events
        public Action<ClipLogItem>? AddClipToReportCallback { get; set; }
        public event Action? FlashMarkInButton;
        public event Action? FlashMarkOutButton;
        public event Action? FlashTagClipButton;

        // --- FILE NAVIGATION ---
        private readonly FileNavigationService _fileNavigationService = new();
        private static readonly string[] VideoExtensions = { ".mov", ".mp4", ".mxf", ".avi", ".mkv", ".m4v", ".mpg", ".mpeg" };
        private List<string> _siblingFiles = new();
        private int _currentFileIndex = -1;

        
        // --- REPORT NOTE EDITING ---
        private ReportItem? _linkedReportItem;

        [ObservableProperty]
        private string? _currentNote;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateReportCommand))]
        private bool _canEditNote;

        // Callback to find if current file is in Report
        public Func<string, ReportItem?>? GetReportItemCallback;


        // Timer for updating UI slider/time
        private readonly DispatcherTimer _uiTimer;
        private bool _isUserSeeking; 
        private bool _isFromTimer; 
        
        public VideoPlayerViewModel()
        {
            // USE SHARED ENGINE
            // Note: DesignMode check removed or needs Avalonia equivalent if essential
            var libVLC = VideoEngineService.Instance.LibVLC;
            if (libVLC != null)
            {
                _mediaPlayer = new MediaPlayer(libVLC);
                Player = _mediaPlayer;

                _mediaPlayer.LengthChanged += OnLengthChanged;
                _mediaPlayer.EndReached += OnEndReached;
                _mediaPlayer.EncounteredError += OnError;
            }

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; 
            _uiTimer.Tick += OnUiTick;
        }

        private void OnUiTick(object? sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying && !_isUserSeeking)
            {
                var time = _mediaPlayer.Time;
                
                // Interpolation Logic
                if (time != _lastMediaTime)
                {
                    _lastMediaTime = time;
                    _stopwatch.Restart();
                }

                // Interpolated time
                var interpolatedTime = TimeSpan.FromMilliseconds(_lastMediaTime + _stopwatch.ElapsedMilliseconds);
                
                if (interpolatedTime.TotalMilliseconds > _mediaPlayer.Length)
                    interpolatedTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length);

                var length = _mediaPlayer.Length;

                if (length > 0)
                {
                    _isFromTimer = true;
                    PlaybackPercent = interpolatedTime.TotalMilliseconds / length;
                    _isFromTimer = false;
                }
                
                CurrentTimeDisplay = FormatTimecode(interpolatedTime);
            }
        }

        private string FormatTimecode(TimeSpan time)
        {
            return Services.TimecodeHelper.FormatTimecode(time, _fps, _startHeaderOffset);
        }

        private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalTimeDisplay = FormatTimecode(TimeSpan.FromMilliseconds(e.Length));
            });
        }

        private void OnEndReached(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsPlaying = false; 
                IsPaused = false;
                _uiTimer.Stop();
                PlaybackPercent = 0;
                CurrentTimeDisplay = "00:00:00:00";
                
                _mediaPlayer?.Stop(); 
            });
        }

        private void OnError(object? sender, EventArgs e)
        {
             IsPlaying = false;
             _uiTimer.Stop();
        }


        public async System.Threading.Tasks.Task LoadVideo(string path)
        {
             // FilePath = path; // Set Path so Refresh can use it
             // Actually wait, let LoadMediaContext do it properly.
             
             await LoadMediaContext(path);
             
             RefreshReportLink();
             UpdateSiblingFiles(path);
        }

        public void RefreshReportLink()
        {
            if (string.IsNullOrEmpty(FilePath)) return;

            ReportItem? linkedItem = null;
            if (GetReportItemCallback != null)
            {
                linkedItem = GetReportItemCallback.Invoke(FilePath);
            }

            _linkedReportItem = linkedItem;

            if (_linkedReportItem is ReportItem)
            {
                CanEditNote = true;
            }
            else
            {
                CanEditNote = false;
            }
            
            UpdateReportCommand.NotifyCanExecuteChanged();
        }

        public async Task LoadMediaContext(string path)
        {
            try
            {
                await Stop(); 

                FilePath = path;
                FileName = System.IO.Path.GetFileName(path);
                IsVideoLoaded = true;

                // Load metadata
                await LoadMetadataWithFFprobe(path);

                Services.RecentFilesService.Instance.AddRecentFile(path);

                if (CurrentVideoMetadata.IsProResRAW)
                {
                    IsVideoLoaded = false;
                    // TODO: Replace MessageBox
                    // System.Windows.MessageBox.Show(...)
                    Console.WriteLine("ProRes RAW Not Supported");
                    return;
                }

                var libVLC = VideoEngineService.Instance.LibVLC;

                if (libVLC != null && _mediaPlayer != null)
                {
                    var media = new Media(libVLC, path, FromType.FromPath);
                    media.AddOption(":start-paused");
                    await media.Parse(MediaParseOptions.ParseLocal);
                    _mediaPlayer.Media = media;
                    
                    IsVideoLoaded = true;

                    _mediaPlayer.Play();
                    
                    IsPlaying = false;
                    IsPaused = false;

                    if (media.Duration > 0)
                    {
                         TotalTimeDisplay = FormatTimecode(TimeSpan.FromMilliseconds(media.Duration));
                    }

                    await LoadMetadataWithFFprobe(path);
                    
                    _mediaPlayer.Volume = (int)(Volume * 100);
                    _mediaPlayer.Mute = IsMuted;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading video: {ex.Message}");
            }
        }

        private async Task LoadMetadataWithFFprobe(string path)
        {
             var provider = new FFprobeMetadataProvider();
             CurrentVideoMetadata = await provider.GetVideoMetadataAsync(path);
             
              double parsedFps = Services.TimecodeHelper.ParseFrameRate(CurrentVideoMetadata.FrameRate);
              if (parsedFps > 0) _fps = parsedFps;

              _startHeaderOffset = Services.TimecodeHelper.ParseTimecodeOffset(CurrentVideoMetadata.StartTimecode, _fps);
        }

        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            if (_mediaPlayer != null && IsVideoLoaded)
            {
                _mediaPlayer.Play();
                _mediaPlayer.Mute = IsMuted;
                _mediaPlayer.Volume = (int)(Volume * 100);
                _uiTimer.Start();
                _stopwatch.Restart(); 
                IsPlaying = true;
                IsPaused = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Pause()
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying && IsVideoLoaded)
            {
                _mediaPlayer.Pause();
                _mediaPlayer.Pause(); // Explicit double pause call from original?
                _uiTimer.Stop();
                _stopwatch.Stop();
                IsPaused = true;
                IsPlaying = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void TogglePlayPause()
        {
            if (!IsVideoLoaded) return;
            
            if (IsPlaying) Pause();
            else Play();
        }

        [ObservableProperty]
        private bool _isStopPressed;

        [RelayCommand(CanExecute = nameof(CanStop))]
        private async Task Stop()
        {
            IsStopPressed = true;

            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying) _mediaPlayer.Pause();
                _mediaPlayer.Time = 0;
            }
            
            _uiTimer.Stop();
            _stopwatch.Reset();
            IsPlaying = false;
            IsPaused = false; 
            PlaybackPercent = 0;
            CurrentTimeDisplay = "00:00:00:00";
            
            await Task.Delay(200);
            IsStopPressed = false;
        }

        [RelayCommand]
        private void Rewind()
        {
            if (_mediaPlayer != null && IsVideoLoaded)
            {
                var time = _mediaPlayer.Time - 1000; 
                if (time < 0) time = 0;
                _mediaPlayer.Time = time;
                CurrentTimeDisplay = FormatTimecode(TimeSpan.FromMilliseconds(time));
            }
        }

        [RelayCommand]
        private void Forward()
        {
            if (_mediaPlayer != null && IsVideoLoaded)
            {
                var time = _mediaPlayer.Time + 1000; 
                if (time > _mediaPlayer.Length) time = _mediaPlayer.Length;
                _mediaPlayer.Time = time;
                CurrentTimeDisplay = FormatTimecode(TimeSpan.FromMilliseconds(time));
            }
        }

        partial void OnPlaybackPercentChanged(double value)
        {
            if (!_isFromTimer && _mediaPlayer != null && IsVideoLoaded)
            {
                var length = _mediaPlayer.Length;
                if (length > 0)
                {
                    var seekTime = (long)(value * length);
                    _mediaPlayer.Time = seekTime;
                    CurrentTimeDisplay = FormatTimecode(TimeSpan.FromMilliseconds(seekTime));
                }
            }
        }

        public void BeginSeek() => _isUserSeeking = true;
        public void EndSeek() => _isUserSeeking = false;

        private bool CanPlay() => IsVideoLoaded && !IsPlaying;
        private bool CanStop() => IsVideoLoaded;

        private bool CanUnloadMedia() => IsVideoLoaded;

        [RelayCommand]
        private async Task OpenFile()
        {
            // STUB: OpenFileDialog
            await Task.Delay(1); // placeholder
        }

        [RelayCommand(CanExecute = nameof(CanUnloadMedia))]
        private void UnloadMedia()
        {
             if (_mediaPlayer != null)
             {
                 _mediaPlayer.Stop();
                 _mediaPlayer.Media?.Dispose();
                 _mediaPlayer.Media = null;
             }
             
             IsVideoLoaded = false;
             IsPlaying = false;
             IsPaused = false; 
             FileName = "";
             FilePath = "";
             CurrentTimeDisplay = FormatTimecode(TimeSpan.Zero);
             TotalTimeDisplay = FormatTimecode(TimeSpan.Zero);
             PlaybackPercent = 0;
             CurrentVideoMetadata = new VideoMetadata(); 
        }

        private bool CanSendFileToTranscode() => IsVideoLoaded && !string.IsNullOrEmpty(FilePath);

        [RelayCommand(CanExecute = nameof(CanSendFileToTranscode))]
        private void SendFileToTranscode()
        {
            if (CanSendFileToTranscode())
            {
                RequestTranscode?.Invoke(new[] { FilePath });
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditNote))]
        private void UpdateReport()
        {
             if (_linkedReportItem is ReportItem item)
             {
                 // STUB: ReportNoteWindow
                 // var window = new Veriflow.Avalonia.Views.ReportNoteWindow(item);
                 // window.ShowDialog();
             }
        }

        // --- FRAME ACCURATE NAVIGATION ---
        
        private DispatcherTimer? _jogTimer;
        private DispatcherTimer? _delayTimer;
        private int _frameStepDirection; 
        
        [RelayCommand]
        private void KeyDown(KeyEventArgs e)
        {
            // Avalonia KeyEventArgs logic (IsRepeat not always available directly, but handling simpler)
            // Assuming e has propert Key
            
            if (e.Key == Key.Right)
            {
                StartJog(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                StartJog(-1);
                e.Handled = true;
            }
        }

        [RelayCommand]
        private void KeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Right || e.Key == Key.Left)
            {
                StopJog();
                e.Handled = true;
            }
        }

        private void StartJog(int direction)
        {
             _frameStepDirection = direction;
             PerformFrameStep();

             if (_delayTimer == null)
             {
                 _delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                 _delayTimer.Tick += (s, args) => 
                 {
                     _delayTimer.Stop();
                     StartContinuousJog();
                 };
             }
             _delayTimer.Start();
        }

        private void StartContinuousJog()
        {
             if (_jogTimer == null)
             {
                 _jogTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                 _jogTimer.Tick += (s, args) => PerformFrameStep();
             }
             _jogTimer.Start();
        }

        private void StopJog()
        {
            _delayTimer?.Stop();
            _jogTimer?.Stop();
            _frameStepDirection = 0;
        }

        private void PerformFrameStep()
        {
            if (_mediaPlayer != null && IsVideoLoaded && _frameStepDirection != 0)
            {
                double msPerFrame = 1000.0 / _fps;
                long step = (long)Math.Round(msPerFrame);
                
                if (step < 1) step = 1;

                long targetTime = _mediaPlayer.Time + (step * _frameStepDirection);
                
                if (targetTime < 0) targetTime = 0;
                if (targetTime > _mediaPlayer.Length) targetTime = _mediaPlayer.Length;

                _mediaPlayer.Time = targetTime;
                
                CurrentTimeDisplay = FormatTimecode(TimeSpan.FromMilliseconds(targetTime));
            }
        }

        [RelayCommand]
        private void NextFrame() => SingleFrameStep(1);

        [RelayCommand]
        private void PreviousFrame() => SingleFrameStep(-1);

        private void SingleFrameStep(int direction)
        {
            _frameStepDirection = direction;
            PerformFrameStep();
            _frameStepDirection = 0;
        }

        // --- LOGGING COMMANDS ---

        [RelayCommand]
        private void SetIn()
        {
            if (_mediaPlayer == null || !IsVideoLoaded) return;
            
            var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            var formattedTime = FormatTimecode(currentTime);
            
            if (EditingClip != null)
            {
                EditingClip.InPoint = formattedTime;
                RecalculateClipDuration(EditingClip);
            }
            else
            {
                _currentInPoint = currentTime;
                CurrentMarkInDisplay = formattedTime;
                IsLoggingPending = true;
            }
            
            FlashMarkInButton?.Invoke();
        }

        [RelayCommand]
        private void SetOut()
        {
            if (_mediaPlayer == null || !IsVideoLoaded) return;
            
            var currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            var formattedTime = FormatTimecode(currentTime);
            
            if (EditingClip != null)
            {
                EditingClip.OutPoint = formattedTime;
                RecalculateClipDuration(EditingClip);
            }
            else
            {
                _currentOutPoint = currentTime;
                CurrentMarkOutDisplay = formattedTime;
                IsLoggingPending = true;
            }
            
            FlashMarkOutButton?.Invoke();
        }

        [RelayCommand]
        private void TagClip()
        {
             if (_currentInPoint == null || _currentOutPoint == null) return;
             
             var inTime = _currentInPoint.Value;
             var outTime = _currentOutPoint.Value;

             if (outTime <= inTime) outTime = inTime.Add(TimeSpan.FromSeconds(1));

             var duration = outTime - inTime;

             var clip = new ClipLogItem 
             {
                 InPoint = FormatTimecode(inTime),
                 OutPoint = FormatTimecode(outTime),
                 Duration = duration.ToString(@"hh\:mm\:ss"),
                 Notes = $"Clip {TaggedClips.Count + 1}",
                 SourceFile = FilePath ?? "" 
             };

             TaggedClips.Add(clip);
             ExportLogsCommand.NotifyCanExecuteChanged();

             AddClipToReportCallback?.Invoke(clip);

             _currentInPoint = null;
             _currentOutPoint = null;
             CurrentMarkInDisplay = "xx:xx:xx:xx";
             CurrentMarkOutDisplay = "yy:yy:yy:yy";
             IsLoggingPending = false;
             
             FlashTagClipButton?.Invoke();
        }


        [RelayCommand(CanExecute = nameof(CanExportLogs))]
        private void ExportLogs()
        {
            // STUB: Export Logic
        }

        private bool CanExportLogs() => TaggedClips.Count > 0;

        private string GenerateEdlContent()
        {
            return ""; 
        }

        private string GenerateAleContent()
        {
           return "";
        }

        private string SanitizeReelName(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return "AX";
            string name = System.IO.Path.GetFileNameWithoutExtension(filename);
            if (name.Length > 8) name = name.Substring(0, 8);
            return name.Replace(" ", "").ToUpper();
        }

        // --- CLIP EDITING COMMANDS ---

        [RelayCommand]
        private void EnterEditMode(ClipLogItem clip)
        {
            if (clip == null) return;

            if (EditingClip != null)
            {
                EditingClip.IsEditing = false;
            }

            _originalClipData = new ClipLogItem
            {
                InPoint = clip.InPoint,
                OutPoint = clip.OutPoint,
                Duration = clip.Duration,
                Notes = clip.Notes,
                TagColor = clip.TagColor
            };

            EditingClip = clip;
            EditingClip.IsEditing = true;
        }

        [RelayCommand]
        private void SaveClipEdit()
        {
            if (EditingClip == null) return;

            EditingClip.IsEditing = false;
            EditingClip = null;
            _originalClipData = null;
        }

        [RelayCommand]
        private void CancelClipEdit()
        {
            if (EditingClip == null || _originalClipData == null) return;

            EditingClip.InPoint = _originalClipData.InPoint;
            EditingClip.OutPoint = _originalClipData.OutPoint;
            EditingClip.Duration = _originalClipData.Duration;
            EditingClip.Notes = _originalClipData.Notes;
            EditingClip.TagColor = _originalClipData.TagColor;

            EditingClip.IsEditing = false;
            EditingClip = null;
            _originalClipData = null;
        }

        [RelayCommand]
        private void DeleteClip(ClipLogItem clip)
        {
            if (clip == null) return;

            if (EditingClip == clip)
            {
                EditingClip = null;
                _originalClipData = null;
            }

            TaggedClips.Remove(clip);
            ExportLogsCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        public void ClearLoggedClips()
        {
            if (TaggedClips.Count > 0)
            {
                TaggedClips.Clear();
                ExportLogsCommand.NotifyCanExecuteChanged();
            }
        }

        private void RecalculateClipDuration(ClipLogItem clip)
        {
            if (clip == null) return;

            try
            {
                var inTime = ParseTimecode(clip.InPoint);
                var outTime = ParseTimecode(clip.OutPoint);

                if (inTime.HasValue && outTime.HasValue && outTime > inTime)
                {
                    var duration = outTime.Value - inTime.Value;
                    clip.Duration = duration.ToString(@"hh\:mm\:ss");
                }
            }
            catch {}
        }

        private TimeSpan? ParseTimecode(string timecode)
        {
            if (string.IsNullOrWhiteSpace(timecode)) return null;

            var parts = timecode.Split(':');
            if (parts.Length != 4) return null;

            if (int.TryParse(parts[0], out int hours) &&
                int.TryParse(parts[1], out int minutes) &&
                int.TryParse(parts[2], out int seconds))
            {
                return new TimeSpan(hours, minutes, seconds);
            }

            return null;
        }

        // --- FILE NAVIGATION COMMANDS ---

        [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
        private async Task NavigatePrevious()
        {
            if (_currentFileIndex > 0)
            {
                await LoadVideo(_siblingFiles[_currentFileIndex - 1]);
            }
        }

        private bool CanNavigatePrevious() => _currentFileIndex > 0;

        [RelayCommand(CanExecute = nameof(CanNavigateNext))]
        private async Task NavigateNext()
        {
            if (_currentFileIndex < _siblingFiles.Count - 1)
            {
                await LoadVideo(_siblingFiles[_currentFileIndex + 1]);
            }
        }

        private bool CanNavigateNext() => _currentFileIndex >= 0 && _currentFileIndex < _siblingFiles.Count - 1;

        private void UpdateSiblingFiles(string currentPath)
        {
            (_siblingFiles, _currentFileIndex) = _fileNavigationService.GetSiblingFiles(currentPath, VideoExtensions);
            
            NavigatePreviousCommand.NotifyCanExecuteChanged();
            NavigateNextCommand.NotifyCanExecuteChanged();
        }

        private string FormatTimecodeForEdl(TimeSpan ts)
        {
             double totalSeconds = ts.TotalSeconds;
             int h = (int)ts.TotalHours; 
             int m = ts.Minutes;
             int s = ts.Seconds;
             int frames = (int)Math.Round((totalSeconds - (int)totalSeconds) * _fps);
             if (frames >= _fps) frames = 0;

             return $"{h:D2}:{m:D2}:{s:D2}:{frames:D2}";
        }

        public void Dispose()
        {
            _uiTimer?.Stop();
            _delayTimer?.Stop();
            _jogTimer?.Stop();
            
            if (_mediaPlayer != null)
            {
                _mediaPlayer.LengthChanged -= OnLengthChanged;
                _mediaPlayer.EndReached -= OnEndReached;
                _mediaPlayer.EncounteredError -= OnError;
                
                _mediaPlayer.Media?.Dispose();
                _mediaPlayer.Dispose();
            }
            
            if (_uiTimer != null)
            {
                _uiTimer.Tick -= OnUiTick;
            }
            
            GC.SuppressFinalize(this);
        }
    }
}
