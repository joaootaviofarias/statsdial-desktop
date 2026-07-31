namespace PcSystemMonitorLcd.Cli;

public class AppConfig
{
    public string SerialPort { get; set; } = "COM3";
    public int BaudRate { get; set; } = 115200;
    public int IntervalMs { get; set; } = 1000;
}