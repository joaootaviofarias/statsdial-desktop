namespace PcSystemMonitorLcd.Cli.Hardware
{
    internal sealed record HardwareInfo(
        string CpuName,
        CpuVendor CpuVendor,
        string RamName,
        string GpuName,
        GpuVendor GpuVendor);
}
