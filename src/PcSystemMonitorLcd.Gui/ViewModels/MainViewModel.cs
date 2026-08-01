using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PcSystemMonitorLcd.Gui.Services;

namespace PcSystemMonitorLcd.Gui.ViewModels;

// Helper record to hold GPU info for the UI dropdown
public record GpuDisplayItem(string Id, string Name)
{
    // Avalonia will automatically call ToString() to show this in the ComboBox
    public override string ToString() => $"{Id} - {Name}";
}

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

        var gpus = _metricsReader.GetAvailableGpus()
            .Select(g => new GpuDisplayItem(g.Id, g.Name));

        AvailableGpus = new ObservableCollection<GpuDisplayItem>(gpus);
        SelectedGpu = AvailableGpus.FirstOrDefault();
        Distro = RuntimeInformation.OSDescription;
        CpuName = _metricsReader.GetCpu()?.Name ?? string.Empty;
        RamName = _metricsReader.GetRam()?.Name ?? string.Empty;

        RefreshPorts();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(IntervalMs)
        };
        _timer.Tick += (_, _) => Tick();
    }

    // ----- System info (read-only, bound to the "System Information" panel) -----

    [ObservableProperty] private double _cpuUsagePercent;
    [ObservableProperty] private double _cpuTempCelsius;
    [ObservableProperty] private double _ramUsagePercent;
    [ObservableProperty] private double _ramUsedGb;
    [ObservableProperty] private double _ramTotalGb;
    [ObservableProperty] private double _gpuUsagePercent;
    [ObservableProperty] private double _gpuTempCelsius;
    [ObservableProperty] private string _distro = string.Empty;

    // ----- Hardware names shown as badges next to the CPU / RAM labels -----
    // (GPU name is already available via SelectedGpu.Name, no extra prop needed)
    [ObservableProperty] private string _cpuName = string.Empty;
    [ObservableProperty] private string _ramName = string.Empty;

    public ObservableCollection<double> CpuHistory { get; } = new();
    public ObservableCollection<double> RamHistory { get; } = new();
    public ObservableCollection<double> GpuHistory { get; } = new();

    // ----- GPU selection -----

    // Changed from string to GpuDisplayItem
    [ObservableProperty] private ObservableCollection<GpuDisplayItem> _availableGpus;
    [ObservableProperty] private GpuDisplayItem? _selectedGpu;

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

    private async Task Tick()
    {
        // 1. Capture the currently selected GPU Id on the UI thread
        string? currentGpuId = SelectedGpu?.Id;
        bool isCurrentlyConnected = IsConnected;

        // 2. Run the heavy polling on a background thread so the UI doesn't freeze
        var (cpu, ram, gpu) = await Task.Run(() =>
        {
            var c = _metricsReader.GetCpu();
            var r = _metricsReader.GetRam();
            var g = currentGpuId != null ? _metricsReader.GetGpu(currentGpuId) : null;

            return (c, r, g);
        });

        CpuUsagePercent = cpu?.Metrics?.Usage ?? 0;
        CpuTempCelsius = cpu?.Metrics?.Temp ?? 0;

        if (ram?.Metrics != null)
        {
            RamUsagePercent = ram.Metrics.Usage;
            RamTotalGb = Math.Round(ram.Metrics.TotalGb / 1024.0 / 1024.0 / 1024.0, 1);
            RamUsedGb = Math.Round(ram.Metrics.UsedGb / 1024.0 / 1024.0 / 1024.0, 1);
        }

        if (gpu != null)
        {
            GpuUsagePercent = gpu.Metrics?.Usage ?? 0;
            GpuTempCelsius = gpu.Metrics?.Temp ?? 0;
        }

        // Update charts
        PushHistory(CpuHistory, CpuUsagePercent);
        PushHistory(RamHistory, RamUsagePercent);
        PushHistory(GpuHistory, GpuUsagePercent);

        // Write to Serial Port
        if (isCurrentlyConnected)
        {
            var line = $"CPU:{CpuUsagePercent:0}|RAM:{RamUsagePercent:0}|GPU:{GpuUsagePercent:0}|GPUT:{GpuTempCelsius:0}";

            try
            {
                // Note: If your serial port write blocks for a long time, 
                // you might want to move this inside the Task.Run above as well.
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