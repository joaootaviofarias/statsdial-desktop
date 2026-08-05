using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PcSystemMonitorLcd.Gui.Views;

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
