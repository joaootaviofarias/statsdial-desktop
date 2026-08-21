using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StatsDial.Desktop.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
