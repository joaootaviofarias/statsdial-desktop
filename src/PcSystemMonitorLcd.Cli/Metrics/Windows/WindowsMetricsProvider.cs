using System.Diagnostics;
using System.Runtime.InteropServices;
using PcSystemMonitorLcd.Cli.Hardware;
using PcSystemMonitorLcd.Cli.Metrics.Windows.Cpu;
using PcSystemMonitorLcd.Cli.Metrics.Windows.Gpu;

namespace PcSystemMonitorLcd.Cli.Metrics.Windows;

internal class WindowsMetricsProvider : IPlatformMetricsProvider, IDisposable
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly IWindowsCpuTempReader _cpuTempReader;
    private readonly IWindowsGpuReader _gpuReader;

    public WindowsMetricsProvider(HardwareInfo hardwareInfo)
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        _cpuTempReader = WindowsCpuTempReaderFactory.Create(hardwareInfo.CpuVendor);
        _gpuReader = WindowsGpuReaderFactory.Create(hardwareInfo.GpuVendor);
    }

    public double GetCpuPercent() => Math.Round(_cpuCounter.NextValue(), 0);

    public double GetCpuTempCelsius() => _cpuTempReader.GetTempCelsius();

    public double GetRamPercent()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) return -1;
        return Math.Round((double)status.dwMemoryLoad, 0);
    }

    public double GetGpuPercent() => _gpuReader.GetUsagePercent();

    public void Dispose()
    {
        _cpuCounter.Dispose();
        if (_cpuTempReader is IDisposable cpuDisposable) cpuDisposable.Dispose();
        if (_gpuReader is IDisposable gpuDisposable) gpuDisposable.Dispose();
    }

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
}