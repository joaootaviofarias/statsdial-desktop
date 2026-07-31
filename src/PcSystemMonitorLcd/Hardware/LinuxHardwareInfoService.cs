using System.Diagnostics;

namespace PcSystemMonitorLcd.Hardware;

internal sealed class LinuxHardwareInfoService : IHardwareInfoService
{
    public async Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default)
    {
        var (cpuName, cpuVendor) = ReadCpuInfo();
        var ramName = await ReadRamNameAsync(ct);
        var (gpuName, gpuVendor) = await ReadGpuInfoAsync(ct);

        return new HardwareInfo(cpuName, cpuVendor, ramName, gpuName, gpuVendor);
    }

    private static (string Name, CpuVendor Vendor) ReadCpuInfo()
    {
        string name = "Unknown";
        string vendorId = string.Empty;

        foreach (var line in File.ReadLines("/proc/cpuinfo"))
        {
            if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                name = ExtractValue(line);
            else if (line.StartsWith("vendor_id", StringComparison.OrdinalIgnoreCase))
                vendorId = ExtractValue(line);

            if (name != "Unknown" && vendorId != string.Empty)
                break;
        }

        var vendor = vendorId switch
        {
            "GenuineIntel" => CpuVendor.Intel,
            "AuthenticAMD" => CpuVendor.Amd,
            _ => name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? CpuVendor.Intel
               : name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? CpuVendor.Amd
               : CpuVendor.Unknown
        };

        return (name, vendor);
    }

    private static async Task<string> ReadRamNameAsync(CancellationToken ct)
    {
        try
        {
            var output = await RunCommandAsync("dmidecode", "-t 17", ct);
            string? manufacturer = null, partNumber = null;

            foreach (var block in output.Split("Memory Device", StringSplitOptions.RemoveEmptyEntries))
            {
                if (!block.Contains("Size:") || block.Contains("No Module Installed"))
                    continue;

                manufacturer ??= ExtractDmiField(block, "Manufacturer");
                partNumber ??= ExtractDmiField(block, "Part Number");
                if (manufacturer != null && partNumber != null) break;
            }

            var name = $"{manufacturer} {partNumber}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string? ExtractDmiField(string block, string field)
    {
        foreach (var line in block.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(field + ":", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[(field.Length + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(value) &&
                    !value.Equals("Unknown", StringComparison.OrdinalIgnoreCase) &&
                    !value.Equals("Not Specified", StringComparison.OrdinalIgnoreCase))
                    return value;
            }
        }
        return null;
    }

    private static async Task<(string Name, GpuVendor Vendor)> ReadGpuInfoAsync(CancellationToken ct)
    {
        const string drmPath = "/sys/class/drm";
        if (Directory.Exists(drmPath))
        {
            foreach (var card in Directory.GetDirectories(drmPath, "card*"))
            {
                var vendorPath = Path.Combine(card, "device", "vendor");
                if (!File.Exists(vendorPath)) continue;

                var vendorHex = (await File.ReadAllTextAsync(vendorPath, ct)).Trim();
                var vendor = vendorHex switch
                {
                    "0x10de" => GpuVendor.Nvidia,
                    "0x1002" => GpuVendor.Amd,
                    _ => GpuVendor.Unknown
                };
                if (vendor == GpuVendor.Unknown) continue;

                var name = await ReadGpuFriendlyNameAsync(ct) ?? vendor.ToString();
                return (name, vendor);
            }
        }

        return ("Unknown", GpuVendor.Unknown);
    }

    private static async Task<string?> ReadGpuFriendlyNameAsync(CancellationToken ct)
    {
        try
        {
            var output = await RunCommandAsync("lspci", "-mm", ct);
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("VGA compatible controller", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("3D controller", StringComparison.OrdinalIgnoreCase))
                    return line.Trim();
            }
        }
        catch { }

        return null;
    }

    private static string ExtractValue(string line)
    {
        var idx = line.IndexOf(':');
        return idx < 0 ? string.Empty : line[(idx + 1)..].Trim();
    }

    private static async Task<string> RunCommandAsync(string fileName, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return output;
    }
}
