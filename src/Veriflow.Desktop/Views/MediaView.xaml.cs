using System.Windows;
using System.Windows.Controls;

namespace Veriflow.Desktop.Views
{
    public partial class MediaView : UserControl
    {
        public MediaView()
        {
            InitializeComponent();
        }

        // Set initial focus when page loads
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        // Ensure the UserControl always has focus for keyboard shortcuts
        private void UserControl_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Always restore focus to ensure keyboard shortcuts work
            // PreviewMouseDown captures events before child controls
            Focus();
        }
    }
}
