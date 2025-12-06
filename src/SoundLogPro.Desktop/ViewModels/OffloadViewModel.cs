using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace SoundLogPro.Desktop.ViewModels
{
    public partial class OffloadViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _sourcePath;

        [ObservableProperty]
        private string? _destination1Path;

        [ObservableProperty]
        private string? _destination2Path;

        [ObservableProperty]
        private string? _logText;

        public OffloadViewModel()
        {
        }

        [RelayCommand]
        private void PickSource()
        {
            var path = PickFolder();
            if (!string.IsNullOrEmpty(path))
            {
                SourcePath = path;
            }
        }

        [RelayCommand]
        private void PickDest1()
        {
            var path = PickFolder();
            if (!string.IsNullOrEmpty(path))
            {
                Destination1Path = path;
            }
        }

        [RelayCommand]
        private void PickDest2()
        {
            var path = PickFolder();
            if (!string.IsNullOrEmpty(path))
            {
                Destination2Path = path;
            }
        }

        [RelayCommand]
        private async Task StartCopy()
        {
            // Placeholder for copy logic
            await Task.CompletedTask;
        }

        private string? PickFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Sélectionner un dossier",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }

            return null;
        }
    }
}
