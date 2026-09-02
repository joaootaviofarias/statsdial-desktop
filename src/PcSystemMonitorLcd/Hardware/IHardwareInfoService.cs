namespace StatsDial.Core.Hardware;

public interface IHardwareInfoService
{
    Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default);
}