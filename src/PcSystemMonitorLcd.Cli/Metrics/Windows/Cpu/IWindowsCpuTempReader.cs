namespace PcSystemMonitorLcd.Cli.Metrics.Windows.Cpu
{
    internal interface IWindowsCpuTempReader
    {
        double GetTempCelsius();
    }
}
