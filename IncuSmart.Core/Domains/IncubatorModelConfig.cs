namespace IncuSmart.Core.Domains
{
    public class IncubatorModelConfig : BaseDomain<BaseStatus>
    {
        public Guid ModelId { get; set; }
        public Guid ConfigId { get; set; }
        public int? Quantity { get; set; }
        public bool? Required { get; set; }
        public decimal? AbsoluteMin { get; set; }
        public decimal? AbsoluteMax { get; set; }
    }
}
