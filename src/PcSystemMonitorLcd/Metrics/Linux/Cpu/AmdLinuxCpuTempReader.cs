namespace PcSystemMonitorLcd.Metrics.Linux.Cpu;

internal sealed class AmdLinuxCpuTempReader : HwmonCpuTempReader
{
    public AmdLinuxCpuTempReader() : base("k10temp") { }
}
