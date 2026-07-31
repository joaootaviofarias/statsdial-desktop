using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd.Metrics.Linux.Gpu;

internal static class LinuxGpuReaderFactory
{
    public static ILinuxGpuReader Create(GpuVendor vendor) => vendor switch
    {
        GpuVendor.Nvidia => new NvidiaLinuxGpuReader(),
        GpuVendor.Amd => new AmdLinuxGpuReader(),
        _ => new NullLinuxGpuReader()
    };
}
