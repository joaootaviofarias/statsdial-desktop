using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

namespace PcSystemMonitorLcd.Gui.Services;

public interface ISerialTransportService : IDisposable
{
    bool IsOpen { get; }
    IReadOnlyList<string> GetAvailablePorts();
    void Open(string portName, int baudRate = 115200);
    void Close();
    void WriteLine(string line);
}

public sealed class SerialTransportService : ISerialTransportService
{
    private SerialPort? _port;

    public bool IsOpen => _port is { IsOpen: true };

    public IReadOnlyList<string> GetAvailablePorts()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        }

        var candidates = new List<string>();
        foreach (var pattern in new[] { "ttyUSB*", "ttyACM*" })
        {
            candidates.AddRange(Directory.GetFiles("/dev", pattern));
        }

        return candidates.OrderBy(p => p).ToArray();
    }

    public void Open(string portName, int baudRate = 115200)
    {
        Close();

        _port = new SerialPort(portName, baudRate)
        {
            NewLine = "\n",
            ReadTimeout = 500,
            WriteTimeout = 500
        };
        _port.Open();
    }

    public void Close()
    {
        if (_port is null) return;

        try
        {
            if (_port.IsOpen) _port.Close();
        }
        catch
        {
            // ignore close-time errors, we're tearing down anyway
        }
        finally
        {
            _port.Dispose();
            _port = null;
        }
    }

    public void WriteLine(string line)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("Serial port is not open.");

        _port.WriteLine(line);
    }

    public void Dispose() => Close();
}