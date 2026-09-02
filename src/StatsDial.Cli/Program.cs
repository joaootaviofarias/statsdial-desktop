using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StatsDial;
using StatsDial.Cli;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureSerilogLogging()
    .ConfigurePlatformService()
    .ConfigureServices((context, services) =>
    {
        services.AddAppConfig(context.Configuration);
        services.AddSystemMetricsReader();
        services.AddHostedService<MetricsWorker>();
    });

await builder.Build().RunAsync();
