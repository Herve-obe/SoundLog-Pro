using System.Windows;
using System.Windows.Input;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Views
{
    public partial class QuickEditReportWindow : Window
    {
        public bool Saved { get; private set; } = false;

        public QuickEditReportWindow(ReportItem item)
        {
            InitializeComponent();
            DataContext = item;

            // Enable Dragging
            MouseLeftButtonDown += (s, e) => DragMove();
            
            // ESC to Cancel
            PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Saved = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Saved = false;
            Close();
        }

        private void NotesTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Shift+Enter = newline (default behavior)
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    return;
                }
                
                // ENTER alone = Save
                e.Handled = true;
                SaveButton_Click(sender, e);
            }
        }
    }
}
