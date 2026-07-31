namespace PcSystemMonitorLcd.Metrics.Linux.Gpu;

internal sealed class NvidiaLinuxGpuReader : ILinuxGpuReader
{
    public double GetUsagePercent() => NvidiaGpuHelper.TryGetUtilization();
}
