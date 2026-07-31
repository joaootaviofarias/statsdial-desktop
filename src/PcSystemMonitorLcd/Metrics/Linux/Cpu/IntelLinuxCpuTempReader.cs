namespace PcSystemMonitorLcd.Metrics.Linux.Cpu;

internal sealed class IntelLinuxCpuTempReader : HwmonCpuTempReader
{
    public IntelLinuxCpuTempReader() : base("coretemp") { }
}
