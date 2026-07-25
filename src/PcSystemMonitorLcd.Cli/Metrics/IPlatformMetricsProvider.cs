namespace PcSystemMonitorLcd.Cli.Metrics
{
    public interface IPlatformMetricsProvider
    {
        double GetCpuPercent();
        double GetCpuTempCelsius();
        double GetRamPercent();
        double GetGpuPercent();
    }
}
