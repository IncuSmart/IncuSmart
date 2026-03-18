namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("warranties")]
    public class WarrantyEntity : BaseEntity<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Notes { get; set; }
    }
}
