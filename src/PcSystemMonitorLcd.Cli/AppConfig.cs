namespace PcSystemMonitorLcd.Cli;

public class AppConfig
{
    public string SerialPort    { get; set; } = "/dev/ttyUSB0";
    public int    BaudRate      { get; set; } = 115200;
    public int    IntervalMs    { get; set; } = 1000;
    public string GpuDriver     { get; set; } = "auto"; // "nvidia", "amd", "none", 
}