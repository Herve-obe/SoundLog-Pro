using System;
using System.IO;
using System.Text.Json;
using Veriflow.Avalonia.Models;

namespace Veriflow.Avalonia.Services
{
    /// <summary>
    /// Service for managing application settings persistence
    /// </summary>
    public class SettingsService
    {
        private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
        private readonly string _settingsFilePath;
        private AppSettings? _currentSettings;
        private readonly object _lock = new();

        public static SettingsService Instance => _instance.Value;

        private SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var veriflowFolder = Path.Combine(appDataPath, "Veriflow");
            
            // Ensure directory exists
            Directory.CreateDirectory(veriflowFolder);
            
            _settingsFilePath = Path.Combine(veriflowFolder, "settings.json");
        }

        /// <summary>
        /// Gets the current settings, loading from disk if necessary
        /// </summary>
        public AppSettings GetSettings()
        {
            lock (_lock)
            {
                if (_currentSettings == null)
                {
                    _currentSettings = LoadSettings();
                }
                return _currentSettings.Clone();
            }
        }

        /// <summary>
        /// Saves settings to disk
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            lock (_lock)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };

                    var json = JsonSerializer.Serialize(settings, options);
                    File.WriteAllText(_settingsFilePath, json);
                    
                    _currentSettings = settings.Clone();
                }
                catch (Exception ex)
                {
                    // Log error but don't crash
                    System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Loads settings from disk, returns defaults if file doesn't exist or is corrupted
        /// </summary>
        private AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    // First run - create default settings
                    var defaultSettings = new AppSettings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                // Corrupted file or other error - return defaults
                System.Diagnostics.Debug.WriteLine($"Failed to load settings, using defaults: {ex.Message}");
                return new AppSettings();
            }
        }

        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            var defaultSettings = new AppSettings();
            SaveSettings(defaultSettings);
        }

        /// <summary>
        /// Gets the settings file path (for debugging)
        /// </summary>
        public string GetSettingsFilePath() => _settingsFilePath;
    }
}

