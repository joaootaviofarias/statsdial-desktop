using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace StatsDial.Desktop.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "Version dev" : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    // Allows dragging via the custom title bar
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    // Closes the window (used by both the [X] and the CLOSE button)
    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
