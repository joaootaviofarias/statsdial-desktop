using System.Management;
using System.Runtime.Versioning;

namespace PcSystemMonitorLcd.Hardware;

[SupportedOSPlatform("windows")]
internal sealed class WindowsHardwareInfoService : IHardwareInfoService
{

    public Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default)
    {
        var (cpuName, cpuVendor) = ReadCpuInfo();
        var ramName = ReadRamName();
        var (gpuName, gpuVendor) = ReadGpuInfo();

        return Task.FromResult(new HardwareInfo(cpuName, cpuVendor, ramName, gpuName, gpuVendor));
    }

    private static (string Name, CpuVendor Vendor) ReadCpuInfo()
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

            return (name, vendor);
        }
        return ("Unknown", CpuVendor.Unknown);
    }

    private static string ReadRamName()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, PartNumber FROM Win32_PhysicalMemory");
        foreach (ManagementObject obj in searcher.Get())
        {
            var manufacturer = obj["Manufacturer"]?.ToString()?.Trim();
            var partNumber = obj["PartNumber"]?.ToString()?.Trim();

            var name = $"{manufacturer} {partNumber}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }
        return "Unknown";
    }

    private static (string Name, GpuVendor Vendor) ReadGpuInfo()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterCompatibility FROM Win32_VideoController");
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

            if (vendor != GpuVendor.Unknown)
                return (name, vendor);
        }
        return ("Unknown", GpuVendor.Unknown);
    }
}
