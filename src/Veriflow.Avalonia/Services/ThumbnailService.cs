using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Veriflow.Avalonia.Services
{
    public class ThumbnailService
    {
        private readonly string _cacheDirectory;
        private readonly string _ffmpegPath;
        private static readonly SemaphoreSlim _semaphore = new(3); // Limit to 3 concurrent generations

        public ThumbnailService()
        {
            // Setup Cache Directory: %AppData%/Veriflow/Cache/Thumbnails
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _cacheDirectory = Path.Combine(appData, "Veriflow", "Cache", "Thumbnails");
            
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }

            // Locate FFmpeg with fallback paths
            _ffmpegPath = LocateFFmpeg();
            
            if (string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
            {
                Debug.WriteLine("[ThumbnailService] WARNING: FFmpeg not found. Thumbnails will be disabled.");
                Debug.WriteLine($"[ThumbnailService] Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            }
            else
            {
                Debug.WriteLine($"[ThumbnailService] FFmpeg found at: {_ffmpegPath}");
            }
        }

        private string LocateFFmpeg()
        {
            // Try multiple possible locations
            var searchPaths = new[]
            {
                // Output directory (after build with copy rule)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
                // Alternative base directory
                Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
                // Development: ExternalTools folder (relative to bin)
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ExternalTools", "ffmpeg.exe")),
                // Current directory
                Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe"),
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    Debug.WriteLine($"[ThumbnailService] Found FFmpeg at: {path}");
                    return path;
                }
            }

            Debug.WriteLine("[ThumbnailService] FFmpeg not found in any search path:");
            foreach (var path in searchPaths)
            {
                Debug.WriteLine($"  - Checked: {path}");
            }

            return "";
        }

        public async Task<string?> GetThumbnailAsync(string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                Debug.WriteLine($"[ThumbnailService] Video file not found: {videoPath}");
                return null;
            }

            if (!File.Exists(_ffmpegPath))
            {
                Debug.WriteLine($"[ThumbnailService] FFmpeg not found at: {_ffmpegPath}");
                return null;
            }

            // Generate unique filename based on path hash
            string hash = CreateMd5(videoPath + File.GetLastWriteTime(videoPath).Ticks); // Invalidate if file changes
            string outputFilename = $"{hash}.jpg";
            string outputPath = Path.Combine(_cacheDirectory, outputFilename);

            // Return cached if exists (FAST check before semaphore)
            if (File.Exists(outputPath)) return outputPath;

            await _semaphore.WaitAsync();
            try
            {
                // Double-check cache after wait in case another thread did it
                if (File.Exists(outputPath)) return outputPath;

                // Extract frame at 1 second mark, resize to 320px width (aspect ratio preserved, ensuring even height with -2)
                // -ss 00:00:01 : Seek to 1s
                // -vframes 1 : Output one frame
                // -vf scale=320:-2 : Resize (Safe for YUV420)
                // -y : Overwrite (though we checked existence)
                
                string args = $"-ss 00:00:01 -i \"{videoPath}\" -vframes 1 -vf scale=320:-2 -q:v 2 -y \"{outputPath}\"";

                Debug.WriteLine($"[ThumbnailService] Generating: {outputPath} from {videoPath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true // FFmpeg logs to stderr
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                // Read stderr for debugging
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"[ThumbnailService] FFmpeg Error: {error}");
                }

                if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                {
                    Debug.WriteLine($"[ThumbnailService] Success: {outputPath}");
                    return outputPath;
                }
                else
                {
                    Debug.WriteLine($"[ThumbnailService] Failed. ExitCode: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThumbnailService] Exception: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }

            return null;
        }

        private static string CreateMd5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}

