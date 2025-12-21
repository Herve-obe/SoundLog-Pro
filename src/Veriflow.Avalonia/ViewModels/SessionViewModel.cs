using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using Veriflow.Core.Models;
using Veriflow.Core.Services;

namespace Veriflow.Avalonia.ViewModels
{
    public partial class SessionViewModel : ObservableObject, IDisposable
    {
        private readonly SessionService _sessionService;
        private readonly Func<Session> _captureStateCallback;
        private readonly Action<Session> _restoreStateCallback;
        private readonly DispatcherTimer _autoSaveTimer;
        private const int DefaultAutoSaveIntervalMinutes = 5;

        [ObservableProperty]
        private string? _currentSessionPath;

        [ObservableProperty]
        private bool _isSessionModified;

        [ObservableProperty]
        private string _currentSessionName = "Untitled Session";

        public SessionViewModel(
            Func<Session> captureStateCallback,
            Action<Session> restoreStateCallback)
        {
            _sessionService = new SessionService();
            _captureStateCallback = captureStateCallback ?? throw new ArgumentNullException(nameof(captureStateCallback));
            _restoreStateCallback = restoreStateCallback ?? throw new ArgumentNullException(nameof(restoreStateCallback));
            
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(DefaultAutoSaveIntervalMinutes)
            };
            _autoSaveTimer.Tick += OnAutoSaveTick;
            
            var settings = Services.SettingsService.Instance.GetSettings();
            if (settings.EnableAutoSave)
            {
                _autoSaveTimer.Interval = TimeSpan.FromMinutes(settings.AutoSaveIntervalMinutes);
                _autoSaveTimer.Start();
            }
        }

        [RelayCommand]
        private void NewSession()
        {
            if (IsSessionModified)
            {
                 // Stub MessageBox
                 System.Diagnostics.Debug.WriteLine("Ask to save changes stub");
            }

            var newSession = _sessionService.CreateNewSession();
            _restoreStateCallback(newSession);

            CurrentSessionPath = null;
            CurrentSessionName = "Untitled Session";
            IsSessionModified = false;
        }

        [RelayCommand]
        private async Task OpenSession()
        {
             // Stub OpenFileDialog
             System.Diagnostics.Debug.WriteLine("OpenSession Stub - Need StorageProvider");
             await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SaveSession()
        {
            if (string.IsNullOrEmpty(CurrentSessionPath))
            {
                await SaveSessionAs();
                return;
            }

            await SaveSessionToPath(CurrentSessionPath);
        }

        [RelayCommand]
        private async Task SaveSessionAs()
        {
             // Stub SaveFileDialog
             System.Diagnostics.Debug.WriteLine("SaveSessionAs Stub - Need StorageProvider");
             await Task.CompletedTask;
        }

        private async Task SaveSessionToPath(string filePath)
        {
            try
            {
                var session = _captureStateCallback();
                session.SessionName = Path.GetFileNameWithoutExtension(filePath);

                await _sessionService.SaveSessionAsync(session, filePath);

                CurrentSessionPath = filePath;
                CurrentSessionName = session.SessionName;
                IsSessionModified = false;
                
                 System.Diagnostics.Debug.WriteLine($"Session '{session.SessionName}' saved.");
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public void MarkAsModified()
        {
            IsSessionModified = true;
        }
        
        public event EventHandler? OnAutoSaveCompleted;
        
        private async void OnAutoSaveTick(object? sender, EventArgs e)
        {
            var settings = Services.SettingsService.Instance.GetSettings();
            
            if (!settings.EnableAutoSave)
            {
                _autoSaveTimer.Stop();
                return;
            }
            
            if (!IsSessionModified)
                return;
            
            await AutoSaveSession();
        }
        
        private async Task AutoSaveSession()
        {
            try
            {
                string savePath;
                
                if (!string.IsNullOrEmpty(CurrentSessionPath))
                {
                    savePath = CurrentSessionPath;
                }
                else
                {
                    var settings = Services.SettingsService.Instance.GetSettings();
                    var autoSaveFolder = Path.Combine(settings.DefaultSessionFolder, "AutoSave");
                    Directory.CreateDirectory(autoSaveFolder);
                    
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    savePath = Path.Combine(autoSaveFolder, $"AutoSave_{timestamp}.vfsession");
                }
                
                var session = _captureStateCallback();
                session.SessionName = Path.GetFileNameWithoutExtension(savePath);
                
                await _sessionService.SaveSessionAsync(session, savePath);
                
                if (string.IsNullOrEmpty(CurrentSessionPath))
                {
                    CurrentSessionPath = savePath;
                    CurrentSessionName = session.SessionName;
                }
                
                IsSessionModified = false;
                
                OnAutoSaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
        }
        
        public void StartAutoSave()
        {
            var settings = Services.SettingsService.Instance.GetSettings();
            _autoSaveTimer.Interval = TimeSpan.FromMinutes(settings.AutoSaveIntervalMinutes);
            _autoSaveTimer.Start();
        }
        
        public void StopAutoSave()
        {
            _autoSaveTimer.Stop();
        }
        
        public void UpdateAutoSaveInterval(int minutes)
        {
            _autoSaveTimer.Interval = TimeSpan.FromMinutes(minutes);
        }
        
        public void Dispose()
        {
            _autoSaveTimer?.Stop();
             if (_autoSaveTimer != null)
                 _autoSaveTimer.Tick -= OnAutoSaveTick;
        }
    }
}
