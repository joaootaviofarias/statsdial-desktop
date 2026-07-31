using Microsoft.Extensions.DependencyInjection;
using PcSystemMonitorLcd.Hardware;
using PcSystemMonitorLcd.Metrics.Linux;
using PcSystemMonitorLcd.Metrics.Windows;

namespace PcSystemMonitorLcd;

public static class SystemMetricsServiceCollectionExtensions
{
    public static IServiceCollection AddSystemMetricsReader(this IServiceCollection services)
    {
        services.AddSingleton<IHardwareInfoService>(_ =>
        {
            if (OperatingSystem.IsWindows()) return new WindowsHardwareInfoService();
            if (OperatingSystem.IsLinux()) return new LinuxHardwareInfoService();
            throw new PlatformNotSupportedException(
                $"No {nameof(IHardwareInfoService)} implementation is available for the current OS.");
        });

        services.AddSingleton<ISystemMetricsReader>(sp =>
        {
            var hardwareInfoService = sp.GetRequiredService<IHardwareInfoService>();
            HardwareInfo hardwareInfo = hardwareInfoService.GetHardwareInfoAsync().GetAwaiter().GetResult();

            if (OperatingSystem.IsWindows())
                return new WindowsMetricsProvider(hardwareInfo);
            if (OperatingSystem.IsLinux())
                return new LinuxMetricsProvider(hardwareInfo);

            throw new PlatformNotSupportedException(
                $"No {nameof(ISystemMetricsReader)} implementation is available for the current OS.");
        });

        return services;
    }
}
