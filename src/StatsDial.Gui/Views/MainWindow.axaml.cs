using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using StatsDial.Desktop.ViewModels;

namespace StatsDial.Desktop.Views;

public partial class MainWindow : Window
{
    private AboutWindow? _aboutWindow;
    private TrayIcon _trayIcon = null!;

    public MainWindow()
    {
        InitializeComponent();

        SetupTrayIcon();

        // 2. Listen for DataContext changes to bind the dynamic ToolTip
        this.DataContextChanged += MainWindow_DataContextChanged;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel vm)
        {
            await vm.AutoStartAsync();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!App.IsActuallyExiting)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void SetupTrayIcon()
    {
        // Create the context menu items
        var showItem = new NativeMenuItem { Header = "Show" };
        showItem.Click += Show_Click;

        var aboutItem = new NativeMenuItem { Header = "About" };
        aboutItem.Click += About_Click;

        var exitItem = new NativeMenuItem { Header = "Exit" };
        exitItem.Click += Exit_Click;

        var menu = new NativeMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(aboutItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        // Instantiate the TrayIcon
        _trayIcon = new TrayIcon
        {
            // We can brilliantly re-use the Window icon you already defined in XAML!
            Icon = this.Icon,
            ToolTipText = "Stats Dial",
            Menu = menu
        };

        _trayIcon.Clicked += TrayIcon_Clicked;

        // Register the icon globally to the application
        var trayIcons = new TrayIcons { _trayIcon };
        if (Application.Current != null)
        {
            TrayIcon.SetIcons(Application.Current, trayIcons);
        }
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        // When the DataContext is successfully set to your MainViewModel...
        if (this.DataContext is MainViewModel vm)
        {
            // Set the initial hover text
            _trayIcon.ToolTipText = vm.TraySummaryText;

            // Listen for changes so the tooltip updates in real-time as stats change
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.TraySummaryText))
                {
                    _trayIcon.ToolTipText = vm.TraySummaryText;
                }
            };
        }
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e) => RestoreWindow();

    private void Show_Click(object? sender, EventArgs e) => RestoreWindow();

    private void Exit_Click(object? sender, EventArgs e)
    {
        App.IsActuallyExiting = true;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void About_Click(object? sender, EventArgs e)
    {
        if (_aboutWindow == null || !_aboutWindow.IsVisible)
        {
            _aboutWindow = new AboutWindow();
            _aboutWindow.Show();
        }
        else
        {
            _aboutWindow.Activate();
        }
    }

    private void RestoreWindow()
    {
        this.Show();
        this.Activate();
        if (this.WindowState == WindowState.Minimized)
        {
            this.WindowState = WindowState.Normal;
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseWindow_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}