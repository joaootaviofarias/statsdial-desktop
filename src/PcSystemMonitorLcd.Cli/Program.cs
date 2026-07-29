using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PcSystemMonitorLcd.Cli;
using PcSystemMonitorLcd.Cli.Hardware;
using PcSystemMonitorLcd.Cli.Metrics;
using PcSystemMonitorLcd.Cli.Metrics.Linux;
using PcSystemMonitorLcd.Cli.Metrics.Windows;
using Serilog;

var config = new AppConfig
{
    SerialPort = Environment.GetEnvironmentVariable("SERIAL_PORT") ?? "COM3",
    BaudRate = int.TryParse(Environment.GetEnvironmentVariable("BAUD_RATE"), out int b) ? b : 115200,
    IntervalMs = int.TryParse(Environment.GetEnvironmentVariable("INTERVAL_MS"), out int i) ? i : 1000,
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "service-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

IHardwareInfoService hardwareInfoService = OperatingSystem.IsWindows()
    ? new WindowsHardwareInfoService()
    : OperatingSystem.IsLinux()
        ? new LinuxHardwareInfoService()
        : throw new PlatformNotSupportedException("Unsupported OS platform.");

var hardwareInfo = await hardwareInfoService.GetHardwareInfoAsync();

var builder = Host.CreateDefaultBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.UseWindowsService();
}
else if (OperatingSystem.IsLinux())
{
    builder.UseSystemd();
}

builder
    .UseSerilog()
    .ConfigureServices(services =>
    {
        services.AddSingleton(config);
        services.AddSingleton(hardwareInfo);

        services.AddSingleton<IPlatformMetricsProvider>(_ =>
        {
            if (OperatingSystem.IsWindows())
            {
                return new WindowsMetricsProvider(hardwareInfo);
            }
            else if (OperatingSystem.IsLinux())
            {
                return new LinuxMetricsProvider(config);
            }

            throw new PlatformNotSupportedException("Unsupported OS platform.");
        });

        services.AddSingleton<SystemMetricsReader>();
        services.AddHostedService<MetricsWorker>();
    });

await builder.Build().RunAsync();