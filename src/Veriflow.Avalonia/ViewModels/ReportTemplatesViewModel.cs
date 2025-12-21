using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Avalonia.Controls;
using Veriflow.Core.Models;

namespace Veriflow.Avalonia.ViewModels
{
    public partial class ReportTemplatesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ReportSettings _settings;

        public ReportTemplatesViewModel(ReportSettings settings)
        {
            _settings = settings;
        }

        [RelayCommand]
        private void BrowseLogo()
        {
            // Stub
            System.Diagnostics.Debug.WriteLine("BrowseLogo Stub - Need StorageProvider");
        }

        [RelayCommand]
        private void ClearLogo()
        {
            Settings.CustomLogoPath = "";
            Settings.UseCustomLogo = false;
        }

        [RelayCommand]
        private void SavePreset()
        {
             // Stub
            System.Diagnostics.Debug.WriteLine("SavePreset Stub - Need StorageProvider");
        }

        [RelayCommand]
        private void LoadPreset()
        {
             // Stub
            System.Diagnostics.Debug.WriteLine("LoadPreset Stub - Need StorageProvider");
        }
        
        [RelayCommand]
        private void Close(Window window)
        {
            window?.Close();
        }
    }
}
