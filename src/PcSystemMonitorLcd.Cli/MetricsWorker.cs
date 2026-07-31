namespace PcSystemMonitorLcd.Cli;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public class MetricsWorker : BackgroundService
{
    private readonly AppConfig _config;
    private readonly ISystemMetricsReader _reader;
    private readonly ILogger<MetricsWorker> _logger;

    public MetricsWorker(AppConfig config, ISystemMetricsReader reader, ILogger<MetricsWorker> logger)
    {
        _config = config;
        _reader = reader;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("MetricsWorker started → {Port} @ {Baud} baud, interval {Ms}ms.",
            _config.SerialPort, _config.BaudRate, _config.IntervalMs);

        using var sender = new SerialSender(_config);

        try { sender.EnsureOpen(); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to open serial port."); throw; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                double cpu = _reader.GetCpuPercent();
                double ram = _reader.GetRamPercent();
                double gpu = _reader.GetGpuPercent();
                double cpuTemp = _reader.GetCpuTempCelsius();
                int hour = DateTime.Now.Hour;
                int minute = DateTime.Now.Minute;
                int second = DateTime.Now.Second;

                string payload = $"{cpu},{gpu},{ram},{cpuTemp},{hour},{minute},{second}";

                sender.SendLine(payload);
                _logger.LogDebug("Sent: {Payload}", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in send cycle, will retry on next tick.");
            }

            await Task.Delay(_config.IntervalMs, ct);
        }

        _logger.LogInformation("MetricsWorker stopped.");
    }
}