using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PcSystemMonitorLcd.Cli;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppConfig(this IServiceCollection services, IConfiguration configuration)
    {
        AppConfig config = configuration.GetSection("AppConfig").Get<AppConfig>() ?? new AppConfig();
        services.AddSingleton(config);
        return services;
    }
}
