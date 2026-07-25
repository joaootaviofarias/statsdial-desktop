using PcSystemMonitorLcd.Cli.Hardware;

namespace PcSystemMonitorLcd.Cli.Metrics.Windows.Cpu
{
    internal class WindowsCpuTempReaderFactory
    {
        public static IWindowsCpuTempReader Create(CpuVendor vendor) => vendor switch
        {
            CpuVendor.Amd => new AmdWindowsCpuTempReader(),
            CpuVendor.Intel => new IntelWindowsCpuTempReader(),
            _ => new NullCpuTempReader()
        };
    }
    internal sealed class NullCpuTempReader : IWindowsCpuTempReader
    {
        public double GetTempCelsius() => -1;
    }

}
