namespace StatsDial.Core;

public interface ISystemMetricsReader : IDisposable
{
    Cpu GetCpu();
    Gpu GetGpu(string id);
    Ram GetRam();
    IEnumerable<Gpu> GetAvailableGpus();
}
