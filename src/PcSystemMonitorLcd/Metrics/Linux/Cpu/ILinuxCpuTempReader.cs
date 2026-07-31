namespace PcSystemMonitorLcd.Metrics.Linux.Cpu;

internal interface ILinuxCpuTempReader
{
    double GetTempCelsius();
}
