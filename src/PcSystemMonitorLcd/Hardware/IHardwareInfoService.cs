namespace PcSystemMonitorLcd.Hardware;

public interface IHardwareInfoService
{
    Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default);
}