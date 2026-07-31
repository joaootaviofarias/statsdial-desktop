using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd.Metrics.Linux.Cpu;

internal static class LinuxCpuTempReaderFactory
{
    public static ILinuxCpuTempReader Create(CpuVendor vendor) => vendor switch
    {
        CpuVendor.Amd => new AmdLinuxCpuTempReader(),
        CpuVendor.Intel => new IntelLinuxCpuTempReader(),
        _ => new NullLinuxCpuTempReader()
    };
}
