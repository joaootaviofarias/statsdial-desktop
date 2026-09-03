using Microsoft.Extensions.DependencyInjection;
using StatsDial.Core.Hardware;
using StatsDial.Core.Metrics;

namespace StatsDial.Core;

public static class SystemMetricsServiceCollectionExtensions
{
    public static IServiceCollection AddSystemMetricsReader(this IServiceCollection services)
    {
        services.AddSingleton<ISystemMetricsReader>(sp =>
        {
            var hardwareInfoService = GetOSHardwareInfoService();
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

    private static IHardwareInfoService GetOSHardwareInfoService()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsHardwareInfoService();
        if (OperatingSystem.IsLinux())
            return new LinuxHardwareInfoService();
        throw new PlatformNotSupportedException(
            $"No {nameof(IHardwareInfoService)} implementation is available for the current OS.");
    }
}
