using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using StatsDial.Core;
using StatsDial.Desktop.Services;
using StatsDial.Desktop.ViewModels;
using StatsDial.Desktop.Views;

namespace StatsDial.Desktop;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public static bool IsActuallyExiting { get; set; } = false;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        Resources["BodyFont"] = new FontFamily($"avares://{assemblyName}/Assets/Fonts#Exo 2");
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