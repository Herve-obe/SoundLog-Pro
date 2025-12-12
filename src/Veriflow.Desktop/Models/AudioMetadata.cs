using System.Collections.Generic;

namespace Veriflow.Desktop.Models
{
    public class AudioMetadata
    {
        public string Filename { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty; // e.g. "48000Hz / 24bit"
        public string Scene { get; set; } = string.Empty;
        public string Take { get; set; } = string.Empty;
        public string Tape { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string TimecodeStart { get; set; } = string.Empty; // HH:MM:SS:FF
        public double TimeReferenceSeconds { get; set; } // Absolute start in seconds
        public string Originator { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        
        // --- Wave Agent Parity Fields ---
        public string Project { get; set; } = string.Empty;
        public bool? Circled { get; set; }
        public bool? WildTrack { get; set; }
        public string FrameRate { get; set; } = string.Empty; // e.g. "25 ND"
        public string UBits { get; set; } = string.Empty;     // Hex
        public long SamplesSinceMidnight { get; set; }
        public double TCSampleRate { get; set; }              // For "TC Sample Rate"
        public int DigitizerSampleRate { get; set; }          // For "Digitizer Rate" (usually same as FS)
        
        // Replaced List<string> with rich objects
        public List<TrackInfo> Tracks { get; set; } = new List<TrackInfo>();
        
        public int ChannelCount { get; set; }
        public string ChannelCountString => ChannelCount > 0 ? ChannelCount.ToString() : string.Empty;

        // --- Tech Specs (MP3/General) ---
        // --- Tech Specs (MP3/General) ---
        public string FullPath { get; set; } = string.Empty;    // Full file path
        public string FileSize { get; set; } = string.Empty;
        public string Bitrate { get; set; } = string.Empty;     // e.g. "320 kb/s"
        public string BitrateMode { get; set; } = string.Empty; // e.g. "CBR" or "VBR"
        public string SampleRateString { get; set; } = string.Empty; // e.g. "48000 Hz"
        public string BitDepthString { get; set; } = string.Empty; // e.g. "24 bits"
        public string Codec { get; set; } = string.Empty;       // Legacy / Simple name
        public string CodecName { get; set; } = string.Empty;   // e.g. "pcm_s24le"
        public string ChannelLayout { get; set; } = string.Empty; // e.g. "stereo"
        public string ContainerFormat { get; set; } = string.Empty; // e.g. "wav"
        public string EncodingLibrary { get; set; } = string.Empty; // e.g. "LAME3.100"

        // Backwards compatibility helper if needed (though UI should bind to Tracks)
        public List<string> TrackNames => Tracks.Select(t => t.Name).ToList();
    }

    public class TrackInfo
    {
        public int ChannelIndex { get; set; }
        public int InterleaveIndex { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Function { get; set; } = string.Empty;
    }
}
