using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PcSystemMonitorLcd.Cli;

var config = new AppConfig
{
    SerialPort  = Environment.GetEnvironmentVariable("SERIAL_PORT")  ?? "/dev/ttyACM0",
    BaudRate    = int.TryParse(Environment.GetEnvironmentVariable("BAUD_RATE"),  out int b) ? b : 115200,
    IntervalMs  = int.TryParse(Environment.GetEnvironmentVariable("INTERVAL_MS"), out int i) ? i : 1000,
    GpuDriver   = Environment.GetEnvironmentVariable("GPU_DRIVER")   ?? "auto"
};

await Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureServices(services =>
    {
        services.AddSingleton(config);
        services.AddHostedService<MetricsWorker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSystemdConsole();
    })
    .Build()
    .RunAsync();