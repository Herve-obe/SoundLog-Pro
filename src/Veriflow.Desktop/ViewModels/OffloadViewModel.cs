using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using Veriflow.Desktop.Services;

namespace Veriflow.Desktop.ViewModels
{
    public partial class OffloadViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PickSourceCommand))]
        [NotifyCanExecuteChangedFor(nameof(PickDest1Command))]
        [NotifyCanExecuteChangedFor(nameof(PickDest2Command))]
        [NotifyCanExecuteChangedFor(nameof(StartCopyCommand))]
        [NotifyCanExecuteChangedFor(nameof(DropSourceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DropDest1Command))]
        [NotifyCanExecuteChangedFor(nameof(DropDest2Command))]
        [NotifyCanExecuteChangedFor(nameof(StartCopyCommand))] // Triggers button state update
        private bool _isBusy;

        [ObservableProperty]
        private double _progressValue;
        
        [ObservableProperty]
        private string _timeRemainingDisplay = "--:--";

        [ObservableProperty]
        private string _currentSpeedDisplay = "0 MB/s";

        [ObservableProperty]
        private string _currentHashDisplay = "xxHash64: -";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCopyCommand))]
        private string? _sourcePath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCopyCommand))]
        private string? _destination1Path;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCopyCommand))]
        private string? _destination2Path;

        [ObservableProperty]
        private string? _logText;
        
        [ObservableProperty]
        private int _filesCopiedCount;
        
        [ObservableProperty]
        private int _errorsCount;
        
        private readonly SecureCopyService _secureCopyService;

        public OffloadViewModel()
        {
            _secureCopyService = new SecureCopyService();
        }

        #region Drag & Drop Commands

        [RelayCommand]
        private void DragOver(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void DropSource(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string path = files[0];
                    if (Directory.Exists(path))
                    {
                        SourcePath = path;
                    }
                    else if (File.Exists(path))
                    {
                        SourcePath = Path.GetDirectoryName(path);
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void DropDest1(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string path = files[0];
                    if (Directory.Exists(path))
                    {
                        Destination1Path = path;
                    }
                    else if (File.Exists(path))
                    {
                        Destination1Path = Path.GetDirectoryName(path);
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void DropDest2(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string path = files[0];
                    if (Directory.Exists(path))
                    {
                        Destination2Path = path;
                    }
                    else if (File.Exists(path))
                    {
                        Destination2Path = Path.GetDirectoryName(path);
                    }
                }
            }
        }

        #endregion

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void PickSource()
        {
            var path = PickFolder();
            if (!string.IsNullOrEmpty(path))
            {
                SourcePath = path;
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void PickDest1()
        {
            var path = PickFolder();
            if (!string.IsNullOrEmpty(path))
            {
                Destination1Path = path;
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void PickDest2()
        {
            var path = PickFolder();
            if (!string.IsNullOrEmpty(path))
            {
                Destination2Path = path;
            }
        }

        private CancellationTokenSource? _cts;

        private bool CanInteract() => true; // Always true to allow cancelling

        private bool CanCopy() => IsBusy || (!string.IsNullOrEmpty(SourcePath) && (!string.IsNullOrEmpty(Destination1Path) || !string.IsNullOrEmpty(Destination2Path)));

        public IRelayCommand ToggleCopyCommand => StartCopyCommand;

        [RelayCommand]
        private async Task StartCopy()
        {
            if (IsBusy)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    LogText = "Annulation demandée...";
                }
                return;
            }

            if (string.IsNullOrEmpty(SourcePath) || (string.IsNullOrEmpty(Destination1Path) && string.IsNullOrEmpty(Destination2Path)))
            {
                 MessageBox.Show("Veuillez sélectionner une source et au moins une destination.", "Configuration manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }

            IsBusy = true;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            ProgressValue = 0;
            LogText = "Initialisation...";
            FilesCopiedCount = 0;
            ErrorsCount = 0;
            CurrentSpeedDisplay = "0 MB/s";
            CurrentHashDisplay = "xxHash64: -";
            TimeRemainingDisplay = "--:--";
            
            var reportBuilder = new StringBuilder();
            var processingDate = DateTime.Now;

            reportBuilder.AppendLine("==========================================");
            reportBuilder.AppendLine($"RAPPORT DE COPIE SECURISEE VERIFLOW - {processingDate}");
            reportBuilder.AppendLine("==========================================");
            reportBuilder.AppendLine($"Source      : {SourcePath}");
            reportBuilder.AppendLine($"Destination 1: {Destination1Path ?? "N/A"}");
            reportBuilder.AppendLine($"Destination 2: {Destination2Path ?? "N/A"}");
            reportBuilder.AppendLine("------------------------------------------");
            reportBuilder.AppendLine("DÉTAIL DES FICHIERS (Mode Secure/xxHash64) :");

            try
            {
                var sourceDir = new DirectoryInfo(SourcePath!);
                var files = sourceDir.GetFiles("*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                
                // Calculate total size for global ETA
                long totalBytesToCopy = files.Sum(f => f.Length);
                long totalBytesTransferred = 0;
                
                if (totalFiles == 0)
                {
                    LogText = "Aucun fichier à copier.";
                    MessageBox.Show("Le dossier source est vide.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var file in files)
                {
                    token.ThrowIfCancellationRequested();

                    LogText = $"Securing {file.Name}...";
                    string status = "[OK]";
                    string fileHash = "N/A";
                    string errorDetail = "";
                    long initialBytesTransferred = totalBytesTransferred;
                    
                    var progress = new Progress<CopyProgress>(p =>
                    {
                        // Update UI Speed
                        CurrentSpeedDisplay = $"{p.TransferSpeedMbPerSec:F1} MB/s";
                        
                        // Update Overall Progress based on bytes
                        long currentFileBytes = p.BytesTransferred;
                        long globalBytes = initialBytesTransferred + currentFileBytes;
                        
                        // Avoid division by zero
                        if (totalBytesToCopy > 0)
                        {
                            ProgressValue = (double)globalBytes / totalBytesToCopy * 100;

                            // Calculate ETA
                            double speedBytesPerSec = p.TransferSpeedMbPerSec * 1024 * 1024;
                            if (speedBytesPerSec > 0)
                            {
                                long remainingBytes = totalBytesToCopy - globalBytes;
                                double secondsRemaining = remainingBytes / speedBytesPerSec;
                                TimeSpan timeSpan = TimeSpan.FromSeconds(secondsRemaining);
                                TimeRemainingDisplay = timeSpan.ToString(timeSpan.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
                            }
                        }
                    });
                    
                    CurrentHashDisplay = "Calcul en cours...";

                    // Calculate relative path to keep structure
                    string relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);

                    try 
                    {
                        CopyResult? result = null;

                        // Copy to Dest 1
                        if (!string.IsNullOrEmpty(Destination1Path))
                        {
                            var destFile = Path.Combine(Destination1Path, relativePath);
                            result = await _secureCopyService.CopyFileSecureAsync(file.FullName, destFile, progress, token);
                        }

                        // Copy to Dest 2
                        if (!string.IsNullOrEmpty(Destination2Path))
                        {
                            var destFile2 = Path.Combine(Destination2Path, relativePath);
                            var result2 = await _secureCopyService.CopyFileSecureAsync(file.FullName, destFile2, progress, token);
                            
                            if (result != null && result2 != null && result.SourceHash != result2.SourceHash)
                            {
                                throw new Exception("Hash mismatch between destinations!"); 
                            }
                            result = result2 ?? result;
                        }
                        
                        if (result != null && result.Success)
                        {
                            FilesCopiedCount++;
                            fileHash = result.SourceHash;
                            CurrentHashDisplay = $"xxHash64: {fileHash}";
                        }
                        else
                        {
                            ErrorsCount++;
                            status = "[ERREUR]";
                            errorDetail = "Copie échouée ou incomplète";
                        }
                        
                        // Update global bytes manually if success
                         totalBytesTransferred += file.Length; 
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ErrorsCount++;
                        status = "[ERREUR]";
                        errorDetail = ex.Message;
                    }

                    // Append to report
                    reportBuilder.AppendLine($"{processingDate.ToShortTimeString()} - {relativePath} | {status} | {fileHash} | {errorDetail}");
                }
                
                MessageBox.Show(
                    $"Succès ! {FilesCopiedCount} fichiers sécurisés.\nRapport(s) généré(s) dans le(s) dossier(s) de destination.",
                    "Secure Offload Terminé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                LogText = "Copie annulée par l'utilisateur.";
                TimeRemainingDisplay = "ANNULÉ";
                reportBuilder.AppendLine("ANNULATION PAR UTILISATEUR");
                MessageBox.Show("L'opération a été arrêtée par l'utilisateur.", "Arrêt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"Erreur critique : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                 reportBuilder.AppendLine($"ERREUR CRITIQUE: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
                
                // Save Report
                try 
                {
                    string reportFilename = $"Veriflow_Report_{processingDate:yyyyMMdd_HHmmss}.txt";
                    if (!string.IsNullOrEmpty(Destination1Path))
                         File.WriteAllText(Path.Combine(Destination1Path, reportFilename), reportBuilder.ToString());
                    if (!string.IsNullOrEmpty(Destination2Path))
                         File.WriteAllText(Path.Combine(Destination2Path, reportFilename), reportBuilder.ToString());
                }
                catch { /* Ignore report write errors */ }
            }
        }

        private string? PickFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Sélectionner un dossier",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                return dialog.FolderName;
            }

            return null;
        }
    }
}
