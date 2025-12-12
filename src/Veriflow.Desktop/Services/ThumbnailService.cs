using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Veriflow.Desktop.Services
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

            // Locate FFmpeg
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        }

        public async Task<string?> GetThumbnailAsync(string videoPath)
        {
            if (!File.Exists(videoPath) || !File.Exists(_ffmpegPath)) return null;

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
