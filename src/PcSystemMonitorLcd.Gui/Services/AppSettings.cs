namespace StatsDial.Desktop.Services
{
    public class AppSettings
    {
        public string? LastPort { get; set; }
        public string? LastGpuId { get; set; }
        public bool AutoConnect { get; set; } = true;
        public bool AutoStartMonitoring { get; set; } = true;
    }
}
