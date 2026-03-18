namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("incubator_model_configs")]
    public class IncubatorModelConfigEntity : BaseEntity<BaseStatus>
    {
        public Guid ModelId { get; set; }
        public Guid ConfigId { get; set; }
        public int? Quantity { get; set; }
        public bool? Required { get; set; }
        public decimal? AbsoluteMin { get; set; }
        public decimal? AbsoluteMax { get; set; }
    }
}
