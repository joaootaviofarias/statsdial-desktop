using System.Diagnostics;

namespace StatsDial.Core.Metrics;

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
                UseShellExecute = false,
                CreateNoWindow = true,                  // <-- ADD THIS: Stops the CMD window from appearing
                WindowStyle = ProcessWindowStyle.Hidden // <-- ADD THIS: Extra safety precaution
            })!;
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return double.TryParse(output, out double val) ? Math.Round(val, 1) : -1;
        }
        catch { return -1; }
    }
}
