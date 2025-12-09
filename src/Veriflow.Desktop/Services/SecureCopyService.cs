using System;
using System.IO;
using System.IO.Hashing;
using System.Threading;
using System.Threading.Tasks;

namespace Veriflow.Desktop.Services
{
    public class SecureCopyService
    {
        private const int BufferSize = 1024 * 1024 * 4; // 4MB Buffer

        public async Task<CopyResult> CopyFileSecureAsync(string sourcePath, string destPath, IProgress<CopyProgress> progress, CancellationToken ct)
        {
            var result = new CopyResult { Success = false };
            var fileLength = new FileInfo(sourcePath).Length;
            
            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            try
            {
                using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
                using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);
                
                var xxHash = new XxHash64();
                var buffer = new byte[BufferSize];
                long totalRead = 0;
                var startTime = DateTime.UtcNow;
                int read;

                // Rolling Average Speed Calculation
                var speedHistory = new System.Collections.Generic.Queue<double>();
                const int speedHistoryWindow = 30;
                var lastBlockTime = DateTime.UtcNow;

                while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    // HARD CANCEL CHECK
                    ct.ThrowIfCancellationRequested();

                    // Update Hash
                    xxHash.Append(buffer.AsSpan(0, read));

                    // Write to Dest
                    await destStream.WriteAsync(buffer, 0, read, ct);

                    // Update Progress & Speed
                    totalRead += read;
                    var now = DateTime.UtcNow;
                    var timeSinceLastBlock = (now - lastBlockTime).TotalSeconds;
                    lastBlockTime = now;

                    if (timeSinceLastBlock > 0)
                    {
                        var instantSpeedMbs = (read / 1024.0 / 1024.0) / timeSinceLastBlock;
                        
                        // Add to history
                        if (speedHistory.Count >= speedHistoryWindow)
                            speedHistory.Dequeue();
                        
                        speedHistory.Enqueue(instantSpeedMbs);
                        
                        // Calculate Average
                        var averageSpeedMbs = 0.0;
                        if (speedHistory.Count > 0)
                        {
                            double sum = 0;
                            foreach (var s in speedHistory) sum += s;
                            averageSpeedMbs = sum / speedHistory.Count;
                        }

                        var percentage = (double)totalRead / fileLength * 100;
                        
                        progress?.Report(new CopyProgress
                        {
                            Percentage = percentage,
                            TransferSpeedMbPerSec = averageSpeedMbs,
                            BytesTransferred = totalRead,
                            TotalBytes = fileLength
                        });
                    }
                }

                // Finalize keys
                result.SourceHash = BitConverter.ToString(xxHash.GetCurrentHash()).Replace("-", "").ToLowerInvariant();
                result.Success = true;
                result.AverageSpeed = (fileLength / 1024.0 / 1024.0) / (DateTime.UtcNow - startTime).TotalSeconds;
            }
            catch (OperationCanceledException)
            {
                // Cleanup partial file
                try 
                {
                    if (File.Exists(destPath)) 
                        File.Delete(destPath);
                }
                catch { /* Best effort delete */ }
                
                throw; 
            }
            catch (Exception)
            {
                result.Success = false;
                throw; // Rethrow to let ViewModel handle specific errors
            }

            return result;
        }
    }

    public class CopyResult
    {
        public bool Success { get; set; }
        public string SourceHash { get; set; } = string.Empty;
        public double AverageSpeed { get; set; }
    }

    public class CopyProgress
    {
        public double Percentage { get; set; }
        public double TransferSpeedMbPerSec { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
    }
}
