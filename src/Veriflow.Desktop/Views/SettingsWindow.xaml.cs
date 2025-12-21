using System.Windows;
using Veriflow.Desktop.ViewModels;

namespace Veriflow.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCommand.Execute(null);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelCommand.Execute(null);
            DialogResult = false;
            Close();
        }
    }
}
