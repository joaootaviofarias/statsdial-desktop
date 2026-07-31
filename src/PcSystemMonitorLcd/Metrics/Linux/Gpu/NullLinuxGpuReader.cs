namespace PcSystemMonitorLcd.Metrics.Linux.Gpu;

internal sealed class NullLinuxGpuReader : ILinuxGpuReader
{
    public double GetUsagePercent() => -1;
}
