using Microsoft.Extensions.Hosting;
using Serilog;

internal static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureSerilogLogging(this IHostBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "service-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        return builder.UseSerilog();
    }

    public static IHostBuilder ConfigurePlatformService(this IHostBuilder builder)
    {
        if (OperatingSystem.IsWindows())
        {
            builder.UseWindowsService();
        }
        else if (OperatingSystem.IsLinux())
        {
            builder.UseSystemd();
        }

        return builder;
    }
}