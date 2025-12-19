using System.Windows;
using System.Windows.Input;
using Veriflow.Desktop.Models;

namespace Veriflow.Desktop.Views
{
    public partial class ReportNoteWindow : Window
    {
        public bool Saved { get; private set; } = false;

        public ReportNoteWindow(ReportItem item)
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
                Cancel_Click(sender, e);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Saved = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Saved = false;
            DialogResult = false;
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
                Save_Click(sender, e);
            }
        }
    }
}
