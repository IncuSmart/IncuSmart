namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("hatching_batches")]
    public class HatchingBatchEntity : BaseEntity<BaseStatus>
    {
        public Guid SeasonId { get; set; }
        public int BatchIndex { get; set; }
        public string? Name { get; set; }
        public int DayStart { get; set; }
        public int DayEnd { get; set; }
        public DateTime? ActualStartAt { get; set; }
        public DateTime? ActualEndAt { get; set; }
    }
}
