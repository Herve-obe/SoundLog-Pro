using System.Windows.Controls;

namespace Veriflow.Desktop.Views
{
    public partial class PlayerView : UserControl
    {
        public PlayerView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.Focus();
        }

        protected override void OnPreviewMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            this.Focus();
        }
    }
}
