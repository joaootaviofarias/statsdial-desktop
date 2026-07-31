namespace PcSystemMonitorLcd.Metrics.Linux.Cpu;

internal sealed class NullLinuxCpuTempReader : ILinuxCpuTempReader
{
    public double GetTempCelsius() => -1;
}
