using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace SoundLogPro.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "SoundLog Pro";

        [ObservableProperty]
        private object? _currentView;

        // Placeholder for views (Simulating ViewModels or UserControls for now)
        private readonly object _playerView = new { Content = "Player / Metadata View Placeholder" };
        private readonly object _offloadView = new { Content = "Offload Dashboard View Placeholder" };
        private readonly object _reportsView = new { Content = "Reports View Placeholder" };

        public ICommand NavigatePlayerCommand { get; }
        public ICommand NavigateOffloadCommand { get; }
        public ICommand NavigateReportsCommand { get; }

        public MainViewModel()
        {
            NavigatePlayerCommand = new RelayCommand(() => CurrentView = _playerView);
            NavigateOffloadCommand = new RelayCommand(() => CurrentView = _offloadView);
            NavigateReportsCommand = new RelayCommand(() => CurrentView = _reportsView);

            // Default to Player view
            CurrentView = _playerView;
        }
    }
}
