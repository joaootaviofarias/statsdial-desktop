namespace PcSystemMonitorLcd.Cli.Hardware
{
    internal interface IHardwareInfoService
    {
        Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default);
    }
}
