using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Veriflow.Desktop.Services
{
    public interface ITranscodingService
    {
        Task TranscodeAsync(string sourceFile, string outputFile, TranscodeOptions options, IProgress<double> progress, CancellationToken cancellationToken = default);
    }

    public class TranscodeOptions
    {
        public string Format { get; set; } = "WAV";
        public string SampleRate { get; set; } = "Same as Source";
        public string BitDepth { get; set; } = "Same as Source"; // 16, 24, 32
    }

    public class TranscodingService : ITranscodingService
    {
        public async Task TranscodeAsync(string sourceFile, string outputFile, TranscodeOptions options, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
                throw new FileNotFoundException("Source file not found", sourceFile);

            // Ensure output directory exists
            var outDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);

            // Construct FFmpeg arguments
            // Note: This assumes 'ffmpeg' is in the PATH or bundled. 
            // In a real prod app, we'd locate the executable robustly.
            string ffmpegArgs = BuildArguments(sourceFile, outputFile, options);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardError = true, // FFmpeg outputs progress to Stderr
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            // Basic progress parsing (Parsing Duration and Time from stderr)
            // This is a simplified version.
            
            var tcs = new TaskCompletionSource<bool>();
            
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => 
            {
                if (process.ExitCode == 0) tcs.SetResult(true);
                else tcs.SetException(new Exception($"FFmpeg exited with code {process.ExitCode}"));
            };

            // TODO: Parse stderr for progress updates if needed
            // For now, we mainly launch it.
            
            process.Start();
            
            try 
            {
                await tcs.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { } // Best effort kill
                throw;
            }
        }

        private string BuildArguments(string source, string output, TranscodeOptions options)
        {
            // Basic mapped logic
            // -i source ... output
            
            var args = $"-y -i \"{source}\""; // -y overwrite

            // Audio Filter Chain
            // Sample Rate
            if (int.TryParse(options.SampleRate, out int rate))
            {
                args += $" -ar {rate}";
            }

            // Bit Depth (PCM)
            // wav needs specific codecs like pcm_s16le, pcm_s24le
            if (options.Format.ToUpper() == "WAV")
            {
                if (options.BitDepth == "16-bit") args += " -c:a pcm_s16le";
                else if (options.BitDepth == "24-bit") args += " -c:a pcm_s24le";
                else if (options.BitDepth == "32-bit Float") args += " -c:a pcm_f32le";
            }
            else if (options.Format.ToUpper() == "MP3")
            {
                args += " -c:a libmp3lame -q:a 2"; // High quality VBR
            }
            else if (options.Format.ToUpper() == "FLAC")
            {
                args += " -c:a flac";
                // Bit depth for FLAC handled automatically usually, or can constrain
            }
            else if (options.Format.ToUpper() == "AAC")
            {
                args += " -c:a aac -b:a 256k";
            }
            else if (options.Format.ToUpper() == "OGG")
            {
                args += " -c:a libvorbis -q:a 6";
            }

            args += $" \"{output}\"";
            return args;
        }
    }
}
