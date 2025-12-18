using System.Windows;
using System.Windows.Input;
using System.Linq;
using LibVLCSharp.Shared;

namespace Veriflow.Desktop.Views
{
    public partial class VideoPreviewWindow : Window
    {
        public VideoPreviewWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow window dragging by clicking anywhere on the title bar
            try
            {
                DragMove();
            }
            catch
            {
                // DragMove can throw if window state changes during drag
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set default size and center window
            SetDefaultSize();
        }

        private void SetDefaultSize()
        {
            // Default size for 16:9 video
            Width = 640 + 2;
            Height = 360 + 35 + 2;
            
            // Center window
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }
    }
}
