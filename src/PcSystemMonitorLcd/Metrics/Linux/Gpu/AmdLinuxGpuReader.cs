namespace PcSystemMonitorLcd.Metrics.Linux.Gpu;

internal sealed class AmdLinuxGpuReader : ILinuxGpuReader
{
    private const string SysfsPath = "/sys/class/drm/card0/device/gpu_busy_percent";

    public double GetUsagePercent()
    {
        try
        {
            if (!File.Exists(SysfsPath)) return -1;
            string raw = File.ReadAllText(SysfsPath).Trim();
            return double.TryParse(raw, out double val) ? Math.Round(val, 1) : -1;
        }
        catch
        {
            return -1;
        }
    }
}
