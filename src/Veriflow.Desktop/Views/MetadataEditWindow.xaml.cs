using System.Windows;

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
    }
}
