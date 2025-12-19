using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Veriflow.Desktop.Services
{
    /// <summary>
    /// Service for synchronizing audio and video files using waveform correlation.
    /// Extracts audio from video, performs cross-correlation to find offset.
    /// </summary>
    public class WaveformSyncService
    {
        private readonly string _ffmpegPath;
        private readonly string _tempDir;

        public WaveformSyncService()
        {
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            _tempDir = Path.Combine(Path.GetTempPath(), "Veriflow_WaveformSync");
            Directory.CreateDirectory(_tempDir);
        }

        /// <summary>
        /// Finds the time offset between video and audio files using waveform correlation.
        /// </summary>
        /// <param name="videoPath">Path to video file</param>
        /// <param name="audioPath">Path to audio file</param>
        /// <param name="maxDurationSeconds">Maximum duration to analyze (default: 30s for performance)</param>
        /// <param name="progress">Optional progress callback (0.0 to 1.0)</param>
        /// <returns>Offset in seconds (positive = audio ahead, negative = audio behind)</returns>
        public async Task<double?> FindOffsetAsync(string videoPath, string audioPath, int maxDurationSeconds = 30, IProgress<double>? progress = null)
        {
            try
            {
                progress?.Report(0.1);

                // Extract audio from video
                string videoAudioPath = await ExtractAudioFromVideoAsync(videoPath, maxDurationSeconds);
                progress?.Report(0.4);

                // Extract audio (normalize to same format)
                string normalizedAudioPath = await NormalizeAudioAsync(audioPath, maxDurationSeconds);
                progress?.Report(0.6);

                // Perform cross-correlation
                double? offset = await PerformCrossCorrelationAsync(videoAudioPath, normalizedAudioPath);
                progress?.Report(0.9);

                // Cleanup temp files
                CleanupTempFile(videoAudioPath);
                CleanupTempFile(normalizedAudioPath);

                progress?.Report(1.0);
                return offset;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WaveformSync] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts audio from video file to WAV format (mono, 48kHz, 16-bit)
        /// </summary>
        private async Task<string> ExtractAudioFromVideoAsync(string videoPath, int maxDuration)
        {
            string outputPath = Path.Combine(_tempDir, $"video_audio_{Guid.NewGuid()}.wav");
            
            // FFmpeg: Extract audio, convert to mono 48kHz 16-bit PCM, limit duration
            string args = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 48000 -ac 1 -t {maxDuration} -y \"{outputPath}\"";

            await RunFFmpegAsync(args);
            return outputPath;
        }

        /// <summary>
        /// Normalizes audio file to WAV format (mono, 48kHz, 16-bit)
        /// </summary>
        private async Task<string> NormalizeAudioAsync(string audioPath, int maxDuration)
        {
            string outputPath = Path.Combine(_tempDir, $"normalized_audio_{Guid.NewGuid()}.wav");
            
            // FFmpeg: Convert to mono 48kHz 16-bit PCM, limit duration
            string args = $"-i \"{audioPath}\" -acodec pcm_s16le -ar 48000 -ac 1 -t {maxDuration} -y \"{outputPath}\"";

            await RunFFmpegAsync(args);
            return outputPath;
        }

        /// <summary>
        /// Performs cross-correlation between two WAV files to find offset.
        /// Uses a simplified sliding window approach.
        /// </summary>
        private Task<double?> PerformCrossCorrelationAsync(string wavFile1, string wavFile2)
        {
            return Task.Run<double?>(() =>
            {
                try
                {
                    // Read WAV files
                    var samples1 = ReadWavSamples(wavFile1);
                    var samples2 = ReadWavSamples(wavFile2);

                    if (samples1 == null || samples2 == null || samples1.Length == 0 || samples2.Length == 0)
                        return null;

                    // Perform cross-correlation
                    // For performance, we'll use a simplified approach:
                    // Slide the shorter signal over the longer one and find the best match
                    
                    double[] reference, signal;
                    bool audioIsReference;

                    if (samples1.Length > samples2.Length)
                    {
                        reference = samples1;
                        signal = samples2;
                        audioIsReference = false; // video is reference
                    }
                    else
                    {
                        reference = samples2;
                        signal = samples1;
                        audioIsReference = true; // audio is reference
                    }

                    int maxLag = reference.Length - signal.Length;
                    if (maxLag < 0) return null;

                    double maxCorrelation = double.MinValue;
                    int bestLag = 0;

                    // Sliding window correlation
                    for (int lag = 0; lag < maxLag; lag += 100) // Step by 100 samples for performance
                    {
                        double correlation = CalculateCorrelation(reference, signal, lag);
                        if (correlation > maxCorrelation)
                        {
                            maxCorrelation = correlation;
                            bestLag = lag;
                        }
                    }

                    // Fine-tune around best lag
                    int searchStart = Math.Max(0, bestLag - 100);
                    int searchEnd = Math.Min(maxLag, bestLag + 100);
                    for (int lag = searchStart; lag < searchEnd; lag++)
                    {
                        double correlation = CalculateCorrelation(reference, signal, lag);
                        if (correlation > maxCorrelation)
                        {
                            maxCorrelation = correlation;
                            bestLag = lag;
                        }
                    }

                    // Convert lag (in samples) to seconds
                    double sampleRate = 48000; // We normalized to 48kHz
                    double offsetSeconds = bestLag / sampleRate;

                    // Adjust sign based on which file was reference
                    if (!audioIsReference)
                    {
                        offsetSeconds = -offsetSeconds; // Video was reference, so negate
                    }

                    Debug.WriteLine($"[WaveformSync] Best correlation: {maxCorrelation:F4}, Offset: {offsetSeconds:F3}s");
                    return (double?)offsetSeconds;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WaveformSync] Correlation error: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Calculates correlation between two signals at a given lag
        /// </summary>
        private double CalculateCorrelation(double[] reference, double[] signal, int lag)
        {
            double sum = 0;
            int count = 0;

            for (int i = 0; i < signal.Length; i++)
            {
                if (lag + i < reference.Length)
                {
                    sum += reference[lag + i] * signal[i];
                    count++;
                }
            }

            return count > 0 ? sum / count : 0;
        }

        /// <summary>
        /// Reads WAV file samples (16-bit PCM) and normalizes to -1.0 to 1.0
        /// </summary>
        private double[]? ReadWavSamples(string wavPath)
        {
            try
            {
                using var fs = new FileStream(wavPath, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);

                // Skip WAV header (44 bytes for standard PCM WAV)
                br.ReadBytes(44);

                // Read samples
                var samples = new List<double>();
                while (fs.Position < fs.Length)
                {
                    short sample = br.ReadInt16(); // 16-bit PCM
                    double normalized = sample / 32768.0; // Normalize to -1.0 to 1.0
                    samples.Add(normalized);
                }

                return samples.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WaveformSync] Error reading WAV: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Runs FFmpeg command asynchronously
        /// </summary>
        private async Task RunFFmpegAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg error: {error}");
            }
        }

        /// <summary>
        /// Cleans up temporary file
        /// </summary>
        private void CleanupTempFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        /// <summary>
        /// Cleans up all temporary files in the temp directory
        /// </summary>
        public void CleanupAll()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }
    }
}
