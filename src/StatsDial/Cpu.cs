using StatsDial.Core.Hardware;

namespace StatsDial.Core
{
    public class Cpu
    {
        public string Name { get; set; }
        public CpuVendor Vendor { get; set; }
        public CpuMetric Metrics { get; set; }

    }

    public class CpuMetric
    {
        public double Usage { get; set; }
        public double Temp { get; set; }
    }
}
