using System;

namespace Veriflow.Avalonia.Models
{
    /// <summary>
    /// Represents a recently opened file entry
    /// </summary>
    public class RecentFileEntry
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public RecentFileEntry()
        {
            LastOpened = DateTime.Now;
        }

        public RecentFileEntry(string filePath)
        {
            FilePath = filePath;
            DisplayName = System.IO.Path.GetFileName(filePath);
            LastOpened = DateTime.Now;
        }
    }
}

