using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StatsDial.Core;
using StatsDial.Desktop.Services;

namespace StatsDial.Desktop.ViewModels;

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
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _timer;
    private readonly AppSettings _settings;
    private bool _isLoadingSettings;

    public MainViewModel(
        ISystemMetricsReader metricsReader,
        ISerialTransportService serialTransport,
        ISettingsService settingsService)
    {
        _metricsReader = metricsReader;
        _serialTransport = serialTransport;
        _settingsService = settingsService;
        _settings = _settingsService.Load();

        var gpus = _metricsReader.GetAvailableGpus()
            .Select(g => new GpuDisplayItem(g.Id, g.Name));

        AvailableGpus = new ObservableCollection<GpuDisplayItem>(gpus);

        _isLoadingSettings = true;
        SelectedGpu = AvailableGpus.FirstOrDefault(g => g.Id == _settings.LastGpuId)
                       ?? AvailableGpus.FirstOrDefault();
        _isLoadingSettings = false;

        Distro = RuntimeInformation.OSDescription;
        CpuName = _metricsReader.GetCpu()?.Name ?? string.Empty;
        RamName = _metricsReader.GetRam()?.Name ?? string.Empty;

        RefreshPorts();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(IntervalMs)
        };
        _timer.Tick += async (_, _) => await Tick();
    }

    public string TraySummaryText =>
    $"CPU: {CpuUsagePercent:0}% ({CpuTempCelsius:0}°C) | GPU: {GpuUsagePercent:0}% ({GpuTempCelsius:0}°C) | RAM: {RamUsedGb:0.0} GB";

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

        _isLoadingSettings = true;
        if (_settings.LastPort != null && AvailablePorts.Contains(_settings.LastPort))
        {
            SelectedPort = _settings.LastPort;
        }
        else if (SelectedPort is null || !AvailablePorts.Contains(SelectedPort))
        {
            SelectedPort = AvailablePorts.FirstOrDefault();
        }
        _isLoadingSettings = false;
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
            PersistSettings();
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionDetailText = $"Failed to open {SelectedPort}: {ex.Message}";
        }

        OnPropertyChanged(nameof(ConnectionBadgeText));
    }

    // Auto-fires whenever SelectedPort or SelectedGpu changes (source-generated by CommunityToolkit)
    partial void OnSelectedPortChanged(string? value) => PersistSettings();
    partial void OnSelectedGpuChanged(GpuDisplayItem? value) => PersistSettings();

    private void PersistSettings()
    {
        if (_isLoadingSettings) return; // don't write back the values we just loaded

        _settings.LastPort = SelectedPort;
        _settings.LastGpuId = SelectedGpu?.Id;
        _settingsService.Save(_settings);
    }

    // Called once from the view after it opens, to auto-connect + auto-start
    public async Task AutoStartAsync()
    {
        if (_settings.AutoConnect && SelectedPort != null)
        {
            Connect();
            if (IsConnected && _settings.AutoStartMonitoring && !IsMonitoring)
            {
                ToggleMonitoring();
            }
        }
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
        string? currentGpuId = SelectedGpu?.Id;
        bool isCurrentlyConnected = IsConnected;

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

        PushHistory(CpuHistory, CpuUsagePercent);
        PushHistory(RamHistory, RamUsagePercent);
        PushHistory(GpuHistory, GpuUsagePercent);
        OnPropertyChanged(nameof(TraySummaryText));

        if (isCurrentlyConnected)
        {
            int hour = DateTime.Now.Hour;
            int minute = DateTime.Now.Minute;
            int second = DateTime.Now.Second;

            string payload = $"{cpu.Metrics.Usage},{gpu.Metrics.Usage},{ram.Metrics.Usage},{cpu.Metrics.Temp},{hour},{minute},{second}";

            try
            {
                _serialTransport.WriteLine(payload);
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