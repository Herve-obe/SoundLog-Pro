using System;

namespace Veriflow.Desktop.Models
{
    public class ClipLogItem
    {
        public string InPoint { get; set; } = "";
        public string OutPoint { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Notes { get; set; } = "";
        public string TagColor { get; set; } = "#555555"; // Default grey
    }
}
