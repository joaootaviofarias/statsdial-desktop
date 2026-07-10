using System.Collections.Generic;

namespace PcSystemMonitorLcd.Gui.Services;

public interface IGpuService
{
    IReadOnlyList<string> GetAvailableGpus();
    double GetUsage(string gpuName);
    double GetTemperature(string gpuName);
}


public sealed class GpuService : IGpuService
{
    public IReadOnlyList<string> GetAvailableGpus()
    {
        // TODO: replace with real GPU enumeration from your existing console app
        return new[]
        {
            "GPU 0 (placeholder)",
            "GPU 1 (placeholder)"
        };
    }

    public double GetUsage(string gpuName)
    {
        // TODO: replace with real usage query for the selected GPU
        return 0;
    }

    public double GetTemperature(string gpuName)
    {
        // TODO: replace with real temperature query for the selected GPU
        return 0;
    }
}
