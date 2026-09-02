using System.Management;
using System.Runtime.Versioning;

namespace StatsDial.Core.Hardware;

[SupportedOSPlatform("windows")]
internal sealed class WindowsHardwareInfoService : IHardwareInfoService
{
    public Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default)
    {
        var cpu = ReadCpuInfo();
        var ram = ReadRamInfo();
        var gpus = ReadAllGpus();

        return Task.FromResult(new HardwareInfo(cpu, ram, gpus));
    }

    private static Cpu ReadCpuInfo()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer FROM Win32_Processor");
        foreach (ManagementObject obj in searcher.Get())
        {
            var name = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
            var manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;

            var vendor = manufacturer switch
            {
                "GenuineIntel" => CpuVendor.Intel,
                "AuthenticAMD" => CpuVendor.Amd,
                _ => name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? CpuVendor.Intel
                   : name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? CpuVendor.Amd
                   : CpuVendor.Unknown
            };

            return new Cpu { Name = name, Vendor = vendor };
        }
        return new Cpu { Name = "Unknown", Vendor = CpuVendor.Unknown };
    }

    private static Ram ReadRamInfo()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, PartNumber FROM Win32_PhysicalMemory");
        foreach (ManagementObject obj in searcher.Get())
        {
            var manufacturer = obj["Manufacturer"]?.ToString()?.Trim();
            var partNumber = obj["PartNumber"]?.ToString()?.Trim();

            var name = $"{manufacturer} {partNumber}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return new Ram { Name = name };
            }
        }
        return new Ram { Name = "Unknown" };
    }

    private static IReadOnlyList<Gpu> ReadAllGpus()
    {
        var gpus = new List<Gpu>();
        using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterCompatibility FROM Win32_VideoController");

        int index = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            var name = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
            var compat = obj["AdapterCompatibility"]?.ToString() ?? string.Empty;

            var vendor = compat switch
            {
                var c when c.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) => GpuVendor.Nvidia,
                var c when c.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase)
                        || c.Contains("AMD", StringComparison.OrdinalIgnoreCase) => GpuVendor.Amd,
                _ => name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? GpuVendor.Nvidia
                   : name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ? GpuVendor.Amd
                   : GpuVendor.Unknown
            };

            gpus.Add(new Gpu { Id = index.ToString(), Name = name, Vendor = vendor });
            index++;
        }

        return gpus;
    }
}