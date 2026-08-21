using System.Diagnostics;
using System.Text.RegularExpressions;

namespace StatsDial.Core.Hardware;

internal sealed class LinuxHardwareInfoService : IHardwareInfoService
{
    public async Task<HardwareInfo> GetHardwareInfoAsync(CancellationToken ct = default)
    {
        var cpu = ReadCpuInfo();
        var ram = await ReadRamInfo(ct);
        var gpus = await ReadAllGpusAsync(ct);

        return new HardwareInfo(cpu, ram, gpus);
    }

    private static Cpu ReadCpuInfo()
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

        return new Cpu
        {
            Name = name,
            Vendor = vendor
        };
    }

    private static async Task<Ram> ReadRamInfo(CancellationToken ct)
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

            return new Ram
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name
            };
        }
        catch
        {
            return new Ram
            {
                Name = "Unknown"
            };
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

    private static async Task<IReadOnlyList<Gpu>> ReadAllGpusAsync(CancellationToken ct)
    {
        var gpus = new List<Gpu>();
        const string drmPath = "/sys/class/drm";

        if (!Directory.Exists(drmPath))
        {
            return gpus;
        }

        var pciNames = await ReadPciNamesAsync(ct);
        int nvidiaIndex = 0;

        foreach (var card in Directory.GetDirectories(drmPath, "card*").OrderBy(c => c))
        {
            string cardDirName = Path.GetFileName(card);
            // Skip connector/render sub-nodes like "card0-DP-1" — only bare "cardN" are adapters.
            if (!Regex.IsMatch(cardDirName, @"^card\d+$")) continue;

            string devicePath = Path.Combine(card, "device");
            string vendorPath = Path.Combine(devicePath, "vendor");
            if (!File.Exists(vendorPath)) continue;

            string vendorHex = (await File.ReadAllTextAsync(vendorPath, ct)).Trim();
            GpuVendor vendor = vendorHex switch
            {
                "0x10de" => GpuVendor.Nvidia,
                "0x1002" => GpuVendor.Amd,
                _ => GpuVendor.Unknown
            };

            string? pciAddress = ResolvePciAddress(devicePath);
            string friendlyName = pciAddress is not null && pciNames.TryGetValue(pciAddress, out var n)
                ? n
                : vendor.ToString();

            string cardNumber = cardDirName.Replace("card", "");
            string id = vendor == GpuVendor.Nvidia ? (nvidiaIndex++).ToString() : cardNumber;

            gpus.Add(new Gpu { Id = id, Name = friendlyName, Vendor = vendor });
        }

        return gpus;
    }

    private static string? ResolvePciAddress(string devicePath)
    {
        try
        {
            var target = Directory.ResolveLinkTarget(devicePath, returnFinalTarget: true);
            return target?.Name; // e.g. "0000:01:00.0"
        }
        catch
        {
            return null;
        }
    }

    private static async Task<Dictionary<string, string>> ReadPciNamesAsync(CancellationToken ct)
    {
        var map = new Dictionary<string, string>();
        try
        {
            string output = await RunCommandAsync("lspci", "-mm -D", ct);
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.Contains("VGA compatible controller", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("3D controller", StringComparison.OrdinalIgnoreCase))
                    continue;

                int firstSpace = line.IndexOf(' ');
                if (firstSpace < 0) continue;
                string address = line[..firstSpace].Trim();

                var matches = Regex.Matches(line, "\"([^\"]*)\"");
                if (matches.Count < 3) continue;

                string vendorName = matches[1].Groups[1].Value;
                string deviceName = matches[2].Groups[1].Value;
                map[address] = $"{vendorName} {deviceName}".Trim();
            }
        }
        catch { }
        return map;
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