using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PcSystemMonitorLcd.Gui.Models;

namespace PcSystemMonitorLcd.Gui.Services;

public interface ISystemInfoService
{
    SystemSnapshot GetSnapshot();
}


public sealed class SystemInfoService : ISystemInfoService
{
    private (long idle, long total)? _lastCpuSample;

    public SystemSnapshot GetSnapshot()
    {
        double cpu = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? ReadLinuxCpuUsage()
            : 0; // TODO: plug in your Windows CPU sampling (PerformanceCounter or your existing logic)

        var (usedGb, totalGb) = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? ReadLinuxMemory()
            : (0d, 0d); // TODO: plug in your Windows memory sampling

        return new SystemSnapshot
        {
            CpuUsagePercent = cpu,
            RamUsedGb = usedGb,
            RamTotalGb = totalGb,
            GpuUsagePercent = 0,   // filled in by IGpuService for the selected GPU
            GpuTempCelsius = 0,    // TODO: wire to your GPU temp source
            OsDescription = RuntimeInformation.OSDescription,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };
    }

    private double ReadLinuxCpuUsage()
    {
        try
        {
            var line = File.ReadLines("/proc/stat").First(); // "cpu  1234 0 5678 ..."
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)
                .Select(long.Parse).ToArray();

            long idle = parts[3] + parts[4]; // idle + iowait
            long total = parts.Sum();

            if (_lastCpuSample is { } last)
            {
                long deltaIdle = idle - last.idle;
                long deltaTotal = total - last.total;
                _lastCpuSample = (idle, total);

                if (deltaTotal <= 0) return 0;
                return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
            }

            _lastCpuSample = (idle, total);
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private (double usedGb, double totalGb) ReadLinuxMemory()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            long totalKb = ParseMeminfoLine(lines, "MemTotal:");
            long availableKb = ParseMeminfoLine(lines, "MemAvailable:");

            double totalGb = totalKb / 1024.0 / 1024.0;
            double usedGb = (totalKb - availableKb) / 1024.0 / 1024.0;
            return (usedGb, totalGb);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static long ParseMeminfoLine(string[] lines, string key)
    {
        var line = lines.First(l => l.StartsWith(key));
        var value = line.Replace(key, "").Replace("kB", "").Trim();
        return long.Parse(value);
    }
}
