using PcSystemMonitorLcd.Hardware;
using PcSystemMonitorLcd.Metrics.Linux.Cpu;
using PcSystemMonitorLcd.Metrics.Linux.Gpu;

namespace PcSystemMonitorLcd.Metrics.Linux;

internal class LinuxMetricsProvider : ISystemMetricsReader
{
    private readonly ILinuxCpuTempReader _cpuTempReader;
    private readonly ILinuxGpuReader _gpuReader;
    private CpuSnapshot _lastCpuSnapshot = CpuSnapshot.ReadFromProc();

    public LinuxMetricsProvider(HardwareInfo hardwareInfo)
    {
        _cpuTempReader = LinuxCpuTempReaderFactory.Create(hardwareInfo.CpuVendor);
        _gpuReader = LinuxGpuReaderFactory.Create(hardwareInfo.GpuVendor);
    }

    public double GetCpuPercent()
    {
        var current = CpuSnapshot.ReadFromProc();
        double pct = CpuSnapshot.DeltaPercent(_lastCpuSnapshot, current);
        _lastCpuSnapshot = current;
        return Math.Round(pct, 0);
    }

    public double GetCpuTempCelsius() => _cpuTempReader.GetTempCelsius();

    public double GetRamPercent()
    {
        var info = new Dictionary<string, long>();
        foreach (string line in File.ReadLines("/proc/meminfo"))
        {
            string[] parts = line.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && long.TryParse(parts[1].Replace(" kB", "").Trim(), out long val))
                info[parts[0]] = val;
        }
        if (!info.TryGetValue("MemTotal", out long total) || total == 0) return -1;
        if (!info.TryGetValue("MemAvailable", out long available)) return -1;
        double usedPct = (total - available) / (double)total * 100.0;
        return Math.Round(usedPct, 0);
    }

    public double GetGpuPercent() => _gpuReader.GetUsagePercent();

    public void Dispose()
    {

    }
}