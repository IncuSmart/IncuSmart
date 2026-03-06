namespace IncuSmart.Core.Domains
{
    public class IncubatorConfigInstance : BaseDomain<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public Guid ConfigId { get; set; }
        public int InstanceIndex { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Unit { get; set; }
    }
}
