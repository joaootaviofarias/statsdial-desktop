namespace PcSystemMonitorLcd.Cli.Metrics.Linux
{
    public class LinuxMetricsProvider : IPlatformMetricsProvider
    {
        private readonly AppConfig _config;
        private CpuSnapshot _lastCpuSnapshot = CpuSnapshot.ReadFromProc();

        public LinuxMetricsProvider(AppConfig config) => _config = config;

        public double GetCpuPercent()
        {
            var current = CpuSnapshot.ReadFromProc();
            double pct = CpuSnapshot.DeltaPercent(_lastCpuSnapshot, current);
            _lastCpuSnapshot = current;
            return Math.Round(pct, 0);
        }

        public double GetCpuTempCelsius()
        {
            try
            {
                foreach (string hwmon in Directory.GetDirectories("/sys/class/hwmon", "hwmon*",
                             SearchOption.TopDirectoryOnly).OrderBy(d => d))
                {
                    string namePath = Path.Combine(hwmon, "name");
                    if (!File.Exists(namePath)) continue;
                    if (File.ReadAllText(namePath).Trim() != "k10temp") continue;

                    string tempPath = Path.Combine(hwmon, "temp1_input");
                    if (!File.Exists(tempPath)) continue;
                    if (int.TryParse(File.ReadAllText(tempPath).Trim(), out int milliDeg) && milliDeg > 0)
                        return Math.Round(milliDeg / 1000.0, 0);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CpuTemp] {ex.Message}");
            }
            return -1;
        }

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

        public double GetGpuPercent()
        {
            double nv = NvidiaGpuHelper.TryGetUtilization();
            if (nv >= 0) return nv;

            string driver = _config.GpuDriver.ToLower();
            if (driver is "amd" or "auto")
            {
                double v = TryGetAmdGpu();
                if (v >= 0) return Math.Round(v, 0);
            }
            return -1;
        }

        private static double TryGetAmdGpu()
        {
            try
            {
                string path = "/sys/class/drm/card0/device/gpu_busy_percent";
                if (!File.Exists(path)) return -1;
                string raw = File.ReadAllText(path).Trim();
                return double.TryParse(raw, out double val) ? Math.Round(val, 1) : -1;
            }
            catch { return -1; }
        }
    }
}
