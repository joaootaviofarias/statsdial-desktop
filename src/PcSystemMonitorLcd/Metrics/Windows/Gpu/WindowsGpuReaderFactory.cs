using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd.Metrics.Windows.Gpu;

internal static class WindowsGpuReaderFactory
{
    public static IWindowsGpuReader Create(GpuVendor vendor) => vendor switch
    {
        GpuVendor.Nvidia => new NvidiaWindowsGpuReader(),
        GpuVendor.Amd => new AmdWindowsGpuReader(),
        _ => new NullGpuReader()
    };
}
