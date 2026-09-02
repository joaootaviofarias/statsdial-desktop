namespace StatsDial.Core
{
    public class Ram
    {
        public string Name { get; set; }
        public RamMetric Metrics { get; set; }
    }

    public class RamMetric
    {
        public double Usage { get; set; }
        public double UsedGb { get; set; }
        public double TotalGb { get; set; }
    }
}
