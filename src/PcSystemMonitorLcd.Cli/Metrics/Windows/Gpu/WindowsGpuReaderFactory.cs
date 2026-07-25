using PcSystemMonitorLcd.Cli.Hardware;

namespace PcSystemMonitorLcd.Cli.Metrics.Windows.Gpu
{
    internal static class WindowsGpuReaderFactory
    {
        public static IWindowsGpuReader Create(GpuVendor vendor) => vendor switch
        {
            GpuVendor.Nvidia => new NvidiaWindowsGpuReader(),
            GpuVendor.Amd => new AmdWindowsGpuReader(),
            _ => new NullGpuReader()
        };
    }

    internal sealed class NullGpuReader : IWindowsGpuReader
    {
        public double GetUsagePercent() => -1;
    }
}
