using System.Windows;
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
    }
}
