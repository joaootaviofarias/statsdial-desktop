using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd;

public interface IHardwareInfoService
{
    Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default);
}