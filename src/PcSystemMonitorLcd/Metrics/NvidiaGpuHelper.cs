using System.Diagnostics;

namespace PcSystemMonitorLcd.Metrics;

internal class NvidiaGpuHelper
{
    public static double TryGetUtilization()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=utilization.gpu --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                UseShellExecute = false
            })!;
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return double.TryParse(output, out double val) ? Math.Round(val, 1) : -1;
        }
        catch { return -1; }
    }
}
