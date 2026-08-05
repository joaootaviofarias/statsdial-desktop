using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using PcSystemMonitorLcd.Gui.Services;
using PcSystemMonitorLcd.Gui.ViewModels;
using PcSystemMonitorLcd.Gui.Views;

namespace PcSystemMonitorLcd.Gui;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public static bool IsActuallyExiting { get; set; } = false;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        string orbitronUri = $"avares://{assemblyName}/Assets/Fonts#Orbitron";
        string rajdhaniUri = $"avares://{assemblyName}/Assets/Fonts#Rajdhani";

        Resources["DisplayFont"] = new FontFamily(orbitronUri);
        Resources["BodyFont"] = new FontFamily(rajdhaniUri);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSystemMetricsReader();
        services.AddSingleton<ISerialTransportService, SerialTransportService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<MainViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };

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