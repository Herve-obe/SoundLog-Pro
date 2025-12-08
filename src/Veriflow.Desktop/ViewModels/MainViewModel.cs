using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Veriflow.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Veriflow";

        [ObservableProperty]
        private object? _currentView;

        // ViewModels
        private readonly OffloadViewModel _offloadViewModel = new();
        private readonly PlayerViewModel _playerViewModel = new(); 
        private readonly string _reportsView = "Reports View (Coming Soon)";

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowOffloadCommand { get; }
        public ICommand ShowReportsCommand { get; }

        public MainViewModel()
        {
            ShowDashboardCommand = new RelayCommand(() => CurrentView = _playerViewModel);
            ShowOffloadCommand = new RelayCommand(() => CurrentView = _offloadViewModel);
            ShowReportsCommand = new RelayCommand(() => CurrentView = _reportsView);

            // Default to Player view (MediaBoard)
            CurrentView = _playerViewModel;
        }
    }
}
