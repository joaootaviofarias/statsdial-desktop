namespace PcSystemMonitorLcd;

public interface ISystemMetricsReader : IDisposable
{
    public double GetCpuPercent();
    public double GetCpuTempCelsius();
    public double GetRamPercent();
    public double GetGpuPercent();
}
