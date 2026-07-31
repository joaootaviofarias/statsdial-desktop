namespace PcSystemMonitorLcd.Metrics.Linux.Cpu;

internal class HwmonCpuTempReader : ILinuxCpuTempReader
{
    private readonly string _hwmonName;

    protected HwmonCpuTempReader(string hwmonName) => _hwmonName = hwmonName;

    public double GetTempCelsius()
    {
        try
        {
            foreach (string hwmon in Directory.GetDirectories("/sys/class/hwmon", "hwmon*",
                         SearchOption.TopDirectoryOnly).OrderBy(d => d))
            {
                string namePath = Path.Combine(hwmon, "name");
                if (!File.Exists(namePath)) continue;
                if (File.ReadAllText(namePath).Trim() != _hwmonName) continue;

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
}
