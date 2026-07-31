namespace PcSystemMonitorLcd.Metrics.Windows.Gpu;

internal sealed class NullGpuReader : IWindowsGpuReader
{
    public double GetUsagePercent() => -1;
}
