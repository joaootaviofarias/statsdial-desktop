namespace PcSystemMonitorLcd.Metrics.Windows.Gpu;

internal class NvidiaWindowsGpuReader : IWindowsGpuReader
{
    public double GetUsagePercent() => NvidiaGpuHelper.TryGetUtilization();
}
