namespace PcSystemMonitorLcd.Cli.Metrics;

public class SystemMetricsReader
{
    private readonly IPlatformMetricsProvider _provider;

    public SystemMetricsReader(IPlatformMetricsProvider provider) => _provider = provider;

    public double GetCpuPercent() => _provider.GetCpuPercent();
    public double GetCpuTempCelsius() => _provider.GetCpuTempCelsius();
    public double GetRamPercent() => _provider.GetRamPercent();
    public double GetGpuPercent() => _provider.GetGpuPercent();
}