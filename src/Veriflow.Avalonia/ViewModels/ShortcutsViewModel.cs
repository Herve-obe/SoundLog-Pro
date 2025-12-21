using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Collections.Generic;
using Veriflow.Avalonia.Models;
using System;

namespace Veriflow.Avalonia.ViewModels
{
    public partial class ShortcutsViewModel : ObservableObject
    {
        private readonly ObservableCollection<ShortcutInfo> _allShortcuts = new();
        public ObservableCollection<ShortcutInfo> FilteredShortcuts { get; } = new();

        [ObservableProperty]
        private string _searchText = "";

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilter();
        }

        public ShortcutsViewModel()
        {
            LoadShortcuts();
            UpdateFilter();
        }

        private void UpdateFilter()
        {
            FilteredShortcuts.Clear();
            var query = SearchText?.Trim();

            IEnumerable<ShortcutInfo> result;

            if (string.IsNullOrWhiteSpace(query))
            {
                result = _allShortcuts;
            }
            else
            {
                result = _allShortcuts.Where(info => 
                    info.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                    info.Description.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                    info.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in result)
            {
                FilteredShortcuts.Add(item);
            }
        }

        private void LoadShortcuts()
        {
            _allShortcuts.Add(new ShortcutInfo("Global", "F1", "Go to Offload page"));
            _allShortcuts.Add(new ShortcutInfo("Global", "F2", "Go to Media page"));
            _allShortcuts.Add(new ShortcutInfo("Global", "F3", "Go to Player page"));
            _allShortcuts.Add(new ShortcutInfo("Global", "F4", "Go to Sync page"));
            _allShortcuts.Add(new ShortcutInfo("Global", "F5", "Go to Transcode page"));
            _allShortcuts.Add(new ShortcutInfo("Global", "F6", "Go to Report page"));
            _allShortcuts.Add(new ShortcutInfo("Global", "Ctrl+Tab", "Toggle Audio/Video profile"));
            _allShortcuts.Add(new ShortcutInfo("Global", "F12", "Open Help Manual"));

            _allShortcuts.Add(new ShortcutInfo("Session", "Ctrl+N", "New Session"));
            _allShortcuts.Add(new ShortcutInfo("Session", "Ctrl+O", "Open Session"));
            _allShortcuts.Add(new ShortcutInfo("Session", "Ctrl+S", "Save Session"));
            _allShortcuts.Add(new ShortcutInfo("Session", "Ctrl+Shift+S", "Save Session As"));

            _allShortcuts.Add(new ShortcutInfo("Edit", "Ctrl+Z", "Undo"));
            _allShortcuts.Add(new ShortcutInfo("Edit", "Ctrl+Y", "Redo"));
            _allShortcuts.Add(new ShortcutInfo("Edit", "Ctrl+X", "Cut"));
            _allShortcuts.Add(new ShortcutInfo("Edit", "Ctrl+C", "Copy"));
            _allShortcuts.Add(new ShortcutInfo("Edit", "Ctrl+V", "Paste"));
            _allShortcuts.Add(new ShortcutInfo("Edit", "Delete", "Delete selected item"));

            _allShortcuts.Add(new ShortcutInfo("Player", "Space", "Play/Pause"));
            _allShortcuts.Add(new ShortcutInfo("Player", "Enter", "Stop (return to start)"));
            _allShortcuts.Add(new ShortcutInfo("Player", "← / →", "Frame-by-frame"));
            _allShortcuts.Add(new ShortcutInfo("Player", "↑ / ↓", "Jump 1 second"));
            _allShortcuts.Add(new ShortcutInfo("Player", "B / N", "Prev/Next file"));

            _allShortcuts.Add(new ShortcutInfo("Logging", "I", "Mark In"));
            _allShortcuts.Add(new ShortcutInfo("Logging", "O", "Mark Out"));
            _allShortcuts.Add(new ShortcutInfo("Logging", "T", "Tag clip"));

            _allShortcuts.Add(new ShortcutInfo("Application", "Alt+F4", "Exit"));
        }

        [RelayCommand]
        private void OpenFullManual()
        {
            // Stub
            System.Diagnostics.Debug.WriteLine("OpenFullManual Stub");
        }
    }
}
