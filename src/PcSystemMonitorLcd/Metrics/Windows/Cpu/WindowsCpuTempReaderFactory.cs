using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd.Metrics.Windows.Cpu;

internal class WindowsCpuTempReaderFactory
{
    public static IWindowsCpuTempReader Create(CpuVendor vendor) => vendor switch
    {
        CpuVendor.Amd => new AmdWindowsCpuTempReader(),
        CpuVendor.Intel => new IntelWindowsCpuTempReader(),
        _ => new NullCpuTempReader()
    };
}
