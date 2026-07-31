namespace PcSystemMonitorLcd.Metrics.Windows.Cpu;

internal sealed class NullCpuTempReader : IWindowsCpuTempReader
{
    public double GetTempCelsius() => -1;
}
