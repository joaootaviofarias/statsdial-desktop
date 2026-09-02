using StatsDial.Core.Hardware;

namespace StatsDial.Core
{
    public class Gpu
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public GpuVendor Vendor { get; set; }
        public GpuMetric Metrics { get; set; }
    }

    public class GpuMetric
    {
        public double Usage { get; set; }
        public double Temp { get; set; }
    }
}
