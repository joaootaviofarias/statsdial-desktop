using System.Runtime.InteropServices;

namespace PcSystemMonitorLcd.Metrics.Windows.Cpu;

internal sealed class AmdWindowsCpuTempReader : IWindowsCpuTempReader, IDisposable
{
    private const double MinQueryIntervalSeconds = 1.0;
    private readonly bool _initialized;
    private DateTime _lastQueryUtc = DateTime.MinValue;
    private double _lastTemp = -1;

    public AmdWindowsCpuTempReader()
    {
        try
        {
            _initialized = RmSdk_Init();

            if (!_initialized)
            {
                int step = RmSdk_GetLastStep();
                uint err = RmSdk_GetLastError();
                Console.WriteLine($"[AmdWindowsCpuTempReader] Init failed at step {step}, Win32 error {err} (0x{err:X8})");
            }
        }
        catch (DllNotFoundException e)
        {
            _initialized = false;
        }
        catch (Exception e)
        {
            _initialized = false;
        }
    }

    public double GetTempCelsius()
    {
        if (!_initialized) return -1;

        if ((DateTime.UtcNow - _lastQueryUtc).TotalSeconds < MinQueryIntervalSeconds)
            return _lastTemp;

        _lastQueryUtc = DateTime.UtcNow;
        _lastTemp = RmSdk_GetCpuTemperature(out double temp) ? Math.Round(temp, 0) : -1;
        return _lastTemp;
    }

    public void Dispose()
    {
        if (_initialized) RmSdk_Shutdown();
    }

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