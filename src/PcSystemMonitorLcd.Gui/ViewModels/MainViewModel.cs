using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PcSystemMonitorLcd.Gui.Services;

namespace PcSystemMonitorLcd.Gui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int HistoryLength = 40;

    private readonly ISystemMetricsReader _metricsReader;
    private readonly ISerialTransportService _serialTransport;
    private readonly DispatcherTimer _timer;

    public MainViewModel(
        ISystemMetricsReader metricsReader,
        ISerialTransportService serialTransport)
    {
        _metricsReader = metricsReader;
        _serialTransport = serialTransport;

        AvailableGpus = new ObservableCollection<string>(_metricsReader.GetAvailableGpus().Select(g => g.Id));
        SelectedGpu = AvailableGpus.FirstOrDefault();

        Distro = RuntimeInformation.OSDescription;

        RefreshPorts();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(IntervalMs)
        };
        _timer.Tick += (_, _) => Tick();
    }

    // ----- System info (read-only, bound to the "System Information" panel) -----

    [ObservableProperty] private double _cpuUsagePercent;
    [ObservableProperty] private double _ramUsagePercent;
    [ObservableProperty] private double _ramUsedGb;
    [ObservableProperty] private double _ramTotalGb;
    [ObservableProperty] private double _gpuUsagePercent;
    [ObservableProperty] private double _gpuTempCelsius;
    [ObservableProperty] private string _distro = string.Empty;

    public ObservableCollection<double> CpuHistory { get; } = new();
    public ObservableCollection<double> RamHistory { get; } = new();
    public ObservableCollection<double> GpuHistory { get; } = new();

    // ----- GPU selection -----

    [ObservableProperty] private ObservableCollection<string> _availableGpus;
    [ObservableProperty] private string? _selectedGpu;

    // ----- Serial connection -----

    [ObservableProperty] private ObservableCollection<string> _availablePorts = new();
    [ObservableProperty] private string? _selectedPort;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionDetailText = "Not connected";

    public string ConnectionBadgeText => IsConnected ? "CONNECTED" : "DISCONNECTED";

    // ----- Monitoring loop -----

    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private int _intervalMs = 1000;

    public string MonitoringBadgeText => IsMonitoring ? "LIVE" : "IDLE";
    public string MonitoringButtonText => IsMonitoring ? "STOP" : "START";

    [RelayCommand]
    private void RefreshPorts()
    {
        var ports = _serialTransport.GetAvailablePorts();
        AvailablePorts = new ObservableCollection<string>(ports);
        if (SelectedPort is null || !AvailablePorts.Contains(SelectedPort))
        {
            SelectedPort = AvailablePorts.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void Connect()
    {
        if (SelectedPort is null) return;

        try
        {
            _serialTransport.Open(SelectedPort);
            IsConnected = true;
            ConnectionDetailText = $"Port {SelectedPort}";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionDetailText = $"Failed to open {SelectedPort}: {ex.Message}";
        }

        OnPropertyChanged(nameof(ConnectionBadgeText));
    }

    [RelayCommand]
    private void Disconnect()
    {
        _serialTransport.Close();
        IsConnected = false;
        ConnectionDetailText = "Not connected";
        OnPropertyChanged(nameof(ConnectionBadgeText));
    }

    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _timer.Stop();
            IsMonitoring = false;
        }
        else
        {
            _timer.Interval = TimeSpan.FromMilliseconds(IntervalMs);
            _timer.Start();
            IsMonitoring = true;
        }

        OnPropertyChanged(nameof(MonitoringBadgeText));
        OnPropertyChanged(nameof(MonitoringButtonText));
    }

    private void Tick()
    {
        var cpu = _metricsReader.GetCpu();
        var ram = _metricsReader.GetRam();

        CpuUsagePercent = cpu?.Metrics?.Usage ?? 0;

        if (ram?.Metrics != null)
        {
            RamUsagePercent = ram.Metrics.Usage;
            RamTotalGb = Math.Round(ram.Metrics.TotalGb / 1024.0 / 1024.0 / 1024.0, 1);
            RamUsedGb = Math.Round((ram.Metrics.TotalGb - ram.Metrics.UsedGb) / 1024.0 / 1024.0 / 1024.0, 1);
        }

        if (SelectedGpu is not null)
        {
            var gpu = _metricsReader.GetGpu(SelectedGpu);
            GpuUsagePercent = gpu?.Metrics?.Usage ?? 0;
            GpuTempCelsius = gpu?.Metrics?.Temp ?? 0;
        }

        PushHistory(CpuHistory, CpuUsagePercent);
        PushHistory(RamHistory, RamUsagePercent);
        PushHistory(GpuHistory, GpuUsagePercent);

        if (IsConnected)
        {
            var line = $"CPU:{CpuUsagePercent:0}|RAM:{RamUsagePercent:0}|GPU:{GpuUsagePercent:0}|GPUT:{GpuTempCelsius:0}";

            try
            {
                _serialTransport.WriteLine(line);
            }
            catch (Exception ex)
            {
                ConnectionDetailText = $"Write failed: {ex.Message}";
                IsConnected = false;
                OnPropertyChanged(nameof(ConnectionBadgeText));
            }
        }
    }

    private static void PushHistory(ObservableCollection<double> history, double value)
    {
        history.Add(value);
        while (history.Count > HistoryLength)
            history.RemoveAt(0);
    }

    partial void OnIntervalMsChanged(int value)
    {
        if (_timer.IsEnabled)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(value);
        }
    }
}