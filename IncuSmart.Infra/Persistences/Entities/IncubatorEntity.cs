using IncuSmart.Core.Enums;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("incubators")]
    public class IncubatorEntity : BaseEntity<IncubatorStatus>
    {
        public Guid ModelId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}
