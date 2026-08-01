namespace PcSystemMonitorLcd.Hardware;

public sealed record HardwareInfo(
    Cpu Cpu,
    Ram Ram,
    IReadOnlyList<Gpu> AvailableGpus);
