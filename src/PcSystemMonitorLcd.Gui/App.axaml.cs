using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PcSystemMonitorLcd.Gui.Services;
using PcSystemMonitorLcd.Gui.ViewModels;
using PcSystemMonitorLcd.Gui.Views;

namespace PcSystemMonitorLcd.Gui;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Setup DI container
        var services = new ServiceCollection();

        // Add OS-specific metrics reader
        services.AddSystemMetricsReader();

        // Add Avalonia ViewModels and Services
        services.AddSingleton<ISerialTransportService, SerialTransportService>();
        services.AddTransient<MainViewModel>();

        // 2. Build provider
        Services = services.BuildServiceProvider();

        // 3. Resolve ViewModel and assign it to the Window
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };

            // Dispose of ISystemMetricsReader on exit to release perf counters/sensors gracefully
            desktop.Exit += (s, e) =>
            {
                if (Services is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}