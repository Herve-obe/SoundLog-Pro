namespace Veriflow.Desktop.Models
{
    /// <summary>
    /// Represents a keyboard shortcut
    /// </summary>
    public class ShortcutInfo
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ShortcutInfo() { }

        public ShortcutInfo(string category, string key, string description)
        {
            Category = category;
            Key = key;
            Description = description;
        }
    }
}
