using System.Windows;

namespace Veriflow.Desktop.Helpers
{
    public static class ProMessageBox
    {
        public static bool? Show(string message, string title = "Information", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information)
        {
            // Ensure UI thread access
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => Show(message, title, buttons, image));
            }

            var window = new Views.Shared.ProMessageBox(message, title, buttons, image);
            
            // Safe Owner assignment
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                window.Owner = Application.Current.MainWindow;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            return window.ShowDialog();
        }
        
        // Overloads for convenience matching MessageBox API
        public static bool? Show(string message) 
            => Show(message, "Message", MessageBoxButton.OK, MessageBoxImage.Information);

        public static bool? Show(string message, string title)
            => Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public static bool? Show(string message, string title, MessageBoxButton buttons)
            => Show(message, title, buttons, MessageBoxImage.Information);
    }
}
