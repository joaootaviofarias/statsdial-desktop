namespace PcSystemMonitorLcd.Hardware;

public sealed record HardwareInfo(
    string CpuName,
    CpuVendor CpuVendor,
    string RamName,
    string GpuName,
    GpuVendor GpuVendor);
