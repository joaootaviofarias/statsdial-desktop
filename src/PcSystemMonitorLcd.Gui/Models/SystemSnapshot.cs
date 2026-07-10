using System;

namespace PcSystemMonitorLcd.Gui.Models;

public sealed class SystemSnapshot
{
    public double CpuUsagePercent { get; init; }
    public double RamUsedGb { get; init; }
    public double RamTotalGb { get; init; }
    public double RamUsagePercent => RamTotalGb <= 0 ? 0 : RamUsedGb / RamTotalGb * 100.0;

    public double GpuUsagePercent { get; init; }
    public double GpuTempCelsius { get; init; }

    public string OsDescription { get; init; } = string.Empty;
    public TimeSpan Uptime { get; init; }
}
