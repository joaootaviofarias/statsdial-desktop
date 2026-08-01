using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd.Metrics;

internal class LinuxMetricsProvider : ISystemMetricsReader
{
    private readonly HardwareInfo _hardwareInfo;
    private CpuSnapshot _lastCpuSnapshot = CpuSnapshot.ReadFromProc();

    public LinuxMetricsProvider(HardwareInfo hardwareInfo)
    {
        _hardwareInfo = hardwareInfo;
    }

    public Cpu GetCpu()
    {
        var current = CpuSnapshot.ReadFromProc();
        double usagePct = Math.Round(CpuSnapshot.DeltaPercent(_lastCpuSnapshot, current), 0);
        _lastCpuSnapshot = current;

        if (_hardwareInfo.Cpu != null)
        {
            _hardwareInfo.Cpu.Metrics = new CpuMetric
            {
                Usage = usagePct,
                Temp = GetCpuTempCelsius()
            };
        }

        return _hardwareInfo.Cpu;
    }

    public Gpu GetGpu(string id)
    {
        var targetGpu = _hardwareInfo.AvailableGpus.FirstOrDefault(g => g.Id == id);

        if (targetGpu != null)
        {
            targetGpu.Metrics = new GpuMetric
            {
                Usage = Math.Round(GetGpuPercent(targetGpu), 0),
                Temp = Math.Round(GetGpuTempCelsius(targetGpu), 0)
            };
        }

        return targetGpu;
    }

    public Ram GetRam()
    {
        var ramInfo = GetRamInfoStruct();

        if (_hardwareInfo.Ram != null)
        {
            _hardwareInfo.Ram.Metrics = new RamMetric
            {
                Usage = ramInfo.UsagePercent,
                TotalGb = ramInfo.TotalBytes / (1024 * 1024 * 1024),
                UsedGb = (ramInfo.TotalBytes - ramInfo.AvailableBytes) / (1024 * 1024 * 1024)
            };
        }

        return _hardwareInfo.Ram;
    }

    public IEnumerable<Gpu> GetAvailableGpus()
    {
        return _hardwareInfo.AvailableGpus;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    // --- Vendor-based Routing ---

    private double GetCpuTempCelsius()
    {
        return _hardwareInfo.Cpu.Vendor switch
        {
            CpuVendor.Amd => ReadHwmonTemp("k10temp"),
            CpuVendor.Intel => ReadHwmonTemp("coretemp"),
            _ => -1
        };
    }

    private double GetGpuPercent(Gpu gpu)
    {
        return gpu.Vendor switch
        {
            GpuVendor.Nvidia => GetNvidiaGpuPercent(gpu.Id),
            GpuVendor.Amd => GetAmdGpuPercent(gpu.Id),
            GpuVendor.Intel => GetIntelGpuPercent(gpu.Id),
            _ => -1
        };
    }

    private double GetGpuTempCelsius(Gpu gpu)
    {
        return gpu.Vendor switch
        {
            GpuVendor.Nvidia => GetNvidiaGpuTemp(gpu.Id),
            GpuVendor.Amd => GetAmdGpuTemp(gpu.Id),
            GpuVendor.Intel => GetIntelGpuTemp(gpu.Id),
            _ => -1
        };
    }

    // --- Linux System Helpers ---

    private (long TotalBytes, long AvailableBytes, double UsagePercent) GetRamInfoStruct()
    {
        try
        {
            var info = new Dictionary<string, long>();
            foreach (string line in File.ReadLines("/proc/meminfo"))
            {
                string[] parts = line.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && long.TryParse(parts[1].Replace(" kB", "").Trim(), out long val))
                    info[parts[0]] = val;
            }

            if (!info.TryGetValue("MemTotal", out long totalKb) || totalKb == 0) return (-1, -1, -1);
            if (!info.TryGetValue("MemAvailable", out long availKb)) return (-1, -1, -1);

            long totalBytes = totalKb * 1024;
            long availBytes = availKb * 1024;
            double usagePercent = Math.Round((double)(totalKb - availKb) / totalKb * 100.0, 0);

            return (totalBytes, availBytes, usagePercent);
        }
        catch
        {
            return (-1, -1, -1);
        }
    }

    private double ReadHwmonTemp(string targetName)
    {
        try
        {
            string hwmonPath = "/sys/class/hwmon";
            if (!Directory.Exists(hwmonPath)) return -1;

            foreach (var hwmon in Directory.GetDirectories(hwmonPath))
            {
                string nameFile = Path.Combine(hwmon, "name");
                if (File.Exists(nameFile) && File.ReadAllText(nameFile).Trim() == targetName)
                {
                    string tempFile = Path.Combine(hwmon, "temp1_input");
                    if (File.Exists(tempFile) && double.TryParse(File.ReadAllText(tempFile), out double millidegrees))
                    {
                        return Math.Round(millidegrees / 1000.0, 0);
                    }
                }
            }
        }
        catch { }
        return -1;
    }

    // --- Vendor Implementation Stubs ---

    private double GetNvidiaGpuPercent(string id) => -1;
    private double GetNvidiaGpuTemp(string id) => -1;

    private double GetAmdGpuPercent(string id)
    {
        try
        {
            string path = $"/sys/class/drm/card{id}/device/gpu_busy_percent";
            if (File.Exists(path) && double.TryParse(File.ReadAllText(path).Trim(), out double usage))
            {
                return usage;
            }
        }
        catch { }
        return -1;
    }

    private double GetAmdGpuTemp(string id)
    {
        try
        {
            string hwmonPath = $"/sys/class/drm/card{id}/device/hwmon";
            if (Directory.Exists(hwmonPath))
            {
                foreach (var hwmon in Directory.GetDirectories(hwmonPath))
                {
                    string tempFile = Path.Combine(hwmon, "temp1_input");
                    if (File.Exists(tempFile) && double.TryParse(File.ReadAllText(tempFile), out double millidegrees))
                    {
                        return Math.Round(millidegrees / 1000.0, 0);
                    }
                }
            }
        }
        catch { }
        return -1;
    }

    private double GetIntelGpuPercent(string id) => -1;
    private double GetIntelGpuTemp(string id) => -1;
}