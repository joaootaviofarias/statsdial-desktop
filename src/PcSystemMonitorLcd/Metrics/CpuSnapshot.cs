namespace StatsDial.Core.Metrics;

internal class CpuSnapshot(long User, long Nice, long System, long Idle, long IoWait, long Irq, long SoftIrq)
{
    public long TotalIdle => Idle + IoWait;
    public long TotalActive => User + Nice + System + Irq + SoftIrq;
    public long Total => TotalActive + TotalIdle;

    public static double DeltaPercent(CpuSnapshot prev, CpuSnapshot curr)
    {
        long deltaTotal = curr.Total - prev.Total;
        long deltaIdle = curr.TotalIdle - prev.TotalIdle;

        if (deltaTotal <= 0) return 0.0;
        return (deltaTotal - deltaIdle) / (double)deltaTotal * 100.0;
    }

    public static CpuSnapshot ReadFromProc()
    {
        string line = File.ReadLines("/proc/stat").First(l => l.StartsWith("cpu "));
        long[] v = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(long.Parse)
            .ToArray();

        return new CpuSnapshot(v[0], v[1], v[2], v[3], v[4], v[5], v[6]);
    }
}