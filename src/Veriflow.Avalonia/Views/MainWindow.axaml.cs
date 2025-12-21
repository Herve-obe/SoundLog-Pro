using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Veriflow.Avalonia.ViewModels;

namespace Veriflow.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Setup keyboard shortcuts
            KeyDown += OnKeyDown;
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            switch (e.Key)
            {
                case Key.F1:
                    vm.NavigateToOffloadCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F2:
                    vm.NavigateToMediaCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F3:
                    vm.NavigateToPlayerCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F4:
                    vm.NavigateToSyncCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F5:
                    vm.NavigateToTranscodeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F6:
                    vm.NavigateToReportsCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }
}
