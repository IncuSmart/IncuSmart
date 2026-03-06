using IncuSmart.Core.Enums;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("incubators")]
    public class IncubatorEntity : BaseEntity<BaseStatus>
    {
        public Guid OrderId { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public Guid ModelId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? ActivatedAt { get; set; }
    }
}
