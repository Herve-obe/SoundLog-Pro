using System.Windows;
using System.Windows.Input;

namespace Veriflow.Desktop.Views
{
    public partial class MetadataEditWindow : Window
    {
        public string Project => ProjectBox.Text;
        public string Scene => SceneBox.Text;
        public string Take => TakeBox.Text;
        public string Tape => TapeBox.Text;
        public string Comment => CommentBox.Text;

        public MetadataEditWindow()
        {
            InitializeComponent();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CommentBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Shift+Enter = newline (default behavior)
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    return;
                }
                
                // ENTER alone = Apply
                e.Handled = true;
                Apply_Click(sender, e);
            }
        }
    }
}
