using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PcSystemMonitorLcd.Hardware;

namespace PcSystemMonitorLcd.Metrics;

[SupportedOSPlatform("windows")]
internal class WindowsMetricsProvider : ISystemMetricsReader
{
    private readonly HardwareInfo _hardwareInfo;
    private readonly PerformanceCounter _cpuCounter;
    private const double MinQueryIntervalSeconds = 1.0;
    private bool _amdInitialized;
    private DateTime _amdLastQueryUtc = DateTime.MinValue;
    private double _amdLastTemp = -1;

    public WindowsMetricsProvider(HardwareInfo hardwareInfo)
    {
        _hardwareInfo = hardwareInfo;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        InitCpuTempReader();
    }

    public Cpu GetCpu()
    {
        _hardwareInfo.Cpu.Metrics = new CpuMetric
        {
            Usage = Math.Round(_cpuCounter.NextValue(), 0),
            Temp = GetCpuTempCelsius()
        };

        return _hardwareInfo.Cpu;
    }

    public Gpu GetGpu(string id)
    {
        var targetGpu = _hardwareInfo.AvailableGpus.FirstOrDefault(g => g.Id == id);

        if (targetGpu != null)
        {
            targetGpu.Metrics = new GpuMetric
            {
                Usage = Math.Round(GetGpuPercent(targetGpu), 0)
            };
        }

        return targetGpu;
    }

    public Ram GetRam()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };

        if (GlobalMemoryStatusEx(ref status) && _hardwareInfo.Ram != null)
        {
            _hardwareInfo.Ram.Metrics = new RamMetric
            {
                Usage = Math.Round((double)status.dwMemoryLoad, 0),
                TotalGb = status.ullTotalPhys,
                UsedGb = status.ullTotalPhys - status.ullAvailPhys
            };
        }

        return _hardwareInfo.Ram;
    }

    private double GetGpuPercent(Gpu gpu)
    {
        return gpu.Vendor switch
        {
            GpuVendor.Nvidia => NvidiaGpuHelper.TryGetUtilization(),
            GpuVendor.Amd => -1, // Future AMD GPU implementation
            GpuVendor.Intel => -1, // Future Intel GPU implementation
            _ => -1
        };
    }

    private double GetCpuTempCelsius()
    {
        return _hardwareInfo.Cpu?.Vendor switch
        {
            CpuVendor.Amd => GetAmdCpuTemp(),
            CpuVendor.Intel => -1, // Future Intel CPU temp implementation
            _ => -1
        };
    }

    private double GetAmdCpuTemp()
    {
        if (!_amdInitialized) return -1;

        if ((DateTime.UtcNow - _amdLastQueryUtc).TotalSeconds < MinQueryIntervalSeconds)
            return _amdLastTemp;

        _amdLastQueryUtc = DateTime.UtcNow;
        _amdLastTemp = RmSdk_GetCpuTemperature(out double temp) ? Math.Round(temp, 0) : -1;

        return _amdLastTemp;
    }

    public IEnumerable<Gpu> GetAvailableGpus()
    {
        return _hardwareInfo.AvailableGpus;
    }

    private void InitCpuTempReader()
    {
        // Initialize specific SDKs based on the CPU Vendor
        switch (_hardwareInfo.Cpu?.Vendor)
        {
            case CpuVendor.Amd:
                try
                {
                    _amdInitialized = RmSdk_Init();

                    if (!_amdInitialized)
                    {
                        int step = RmSdk_GetLastStep();
                        uint err = RmSdk_GetLastError();
                        Console.WriteLine($"[WindowsMetricsProvider] AMD Init failed at step {step}, Win32 error {err} (0x{err:X8})");
                    }
                }
                catch (DllNotFoundException)
                {
                    _amdInitialized = false;
                }
                catch (Exception)
                {
                    _amdInitialized = false;
                }
                break;

            case CpuVendor.Intel:
                // Future Intel initialization can go here
                break;
        }
    }

    public void Dispose()
    {
        _cpuCounter.Dispose();

        if (_amdInitialized)
        {
            RmSdk_Shutdown();
        }
    }

    // --- Windows Memory P/Invokes ---

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    // --- AMD Ryzen Master SDK P/Invokes ---

    [DllImport("RmSdkBridge.dll")]
    private static extern bool RmSdk_Init();

    [DllImport("RmSdkBridge.dll")]
    private static extern bool RmSdk_GetCpuTemperature(out double outTemp);

    [DllImport("RmSdkBridge.dll")]
    private static extern void RmSdk_Shutdown();

    [DllImport("RmSdkBridge.dll")]
    private static extern int RmSdk_GetLastStep();

    [DllImport("RmSdkBridge.dll")]
    private static extern uint RmSdk_GetLastError();
}