using System.IO.Ports;

namespace PcSystemMonitorLcd.Cli;

public sealed class SerialSender : IDisposable
{
    private readonly SerialPort _port;

    public SerialSender(AppConfig config)
    {

        var autoPort = DeterminePort(config);

        _port = new SerialPort(autoPort, config.BaudRate)
        {
            NewLine = "\n",
            Encoding = System.Text.Encoding.ASCII,
            WriteTimeout = 1000,
            ReadTimeout = 1000,
            Handshake = Handshake.None,
            RtsEnable = false,
            DtrEnable = true
        };
    }

    public void EnsureOpen()
    {

        try
        {
            if (!_port.IsOpen)
                _port.Open();
        }
        catch (Exception ex) when (ex is TimeoutException || ex is IOException || ex is UnauthorizedAccessException)
        {
            Console.WriteLine($"[Serial] Hardware disconnected ({ex.GetType().Name}). Forcing reconnect...");
            ResetConnection();

            throw;
        }
    }

    public void SendLine(string line)
    {
        EnsureOpen();
        _port.WriteLine(line);
    }

    public void Dispose()
    {
        if (_port.IsOpen) _port.Close();
        _port.Dispose();
    }

    public void ResetConnection()
    {
        Console.WriteLine("[Serial] Forcefully resetting dead connection...");
        try { if (_port.IsOpen) _port.Close(); } catch { }
        try { _port.Dispose(); } catch { }
    }

    private string DeterminePort(AppConfig config)
    {
        if (config.SerialPort.StartsWith("/dev/serial/by-id/"))
        {
            return config.SerialPort;
        }

        try
        {
            string path = "/dev/serial/by-id/";
            if (Directory.Exists(path))
            {
                var devices = Directory.GetFiles(path, "usb-*");

                if (devices.Length > 0)
                {
                    return devices[0];
                }
            }
        }
        catch { }

        return config.SerialPort;
    }
}