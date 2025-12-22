using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Veriflow.Avalonia.Views;

public partial class CustomMessageBox : Window
{
    public CustomMessageBox()
    {
        InitializeComponent();
    }

    public CustomMessageBox(string title, string message) : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
