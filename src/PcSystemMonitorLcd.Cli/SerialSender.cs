using System.IO.Ports;

namespace StatsDial.Cli;

public sealed class SerialSender : IDisposable
{
    private readonly AppConfig _config;
    private readonly object _lock = new();

    private SerialPort? _port;
    private DateTime _lastAttempt = DateTime.MinValue;
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromSeconds(2);

    public SerialSender(AppConfig config)
    {
        _config = config;
    }

    public bool EnsureOpen()
    {
        lock (_lock)
        {
            if (_port is { IsOpen: true })
                return true;

            if (DateTime.UtcNow - _lastAttempt < MinRetryInterval)
                return false;

            _lastAttempt = DateTime.UtcNow;

            DisposePortUnsafe();

            var portName = DeterminePort(_config);

            try
            {
                var port = new SerialPort(portName, _config.BaudRate)
                {
                    NewLine = "\n",
                    Encoding = System.Text.Encoding.ASCII,
                    WriteTimeout = 1000,
                    ReadTimeout = 1000,
                    Handshake = Handshake.None,
                    RtsEnable = false,
                    DtrEnable = true
                };

                port.Open();

                Thread.Sleep(300);

                _port = port;
                Console.WriteLine($"[Serial] Connected on {portName}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Serial] Open failed on {portName} ({ex.GetType().Name}: {ex.Message}). Will retry.");
                DisposePortUnsafe();
                return false;
            }
        }
    }

    public bool SendLine(string line)
    {
        lock (_lock)
        {
            if (!EnsureOpen())
                return false;

            try
            {
                _port!.WriteLine(line);
                return true;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException or ObjectDisposedException or InvalidOperationException)
            {
                Console.WriteLine($"[Serial] Write failed ({ex.GetType().Name}). Marking connection dead, will reconnect next cycle.");
                DisposePortUnsafe();
                return false;
            }
        }
    }

    public bool WaitForPort(TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout;
        var target = DeterminePort(_config);

        while (DateTime.UtcNow < deadline)
        {
            if (SerialPort.GetPortNames().Contains(target, StringComparer.OrdinalIgnoreCase))
                return true;

            Thread.Sleep(interval);
        }

        return SerialPort.GetPortNames().Contains(target, StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            DisposePortUnsafe();
        }
    }

    private void DisposePortUnsafe()
    {
        if (_port == null) return;

        try { if (_port.IsOpen) _port.Close(); } catch { }
        try { _port.Dispose(); } catch { }
        _port = null;
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