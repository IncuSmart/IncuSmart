namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("guest_order_infos")]
    public class GuestOrderInfoEntity : BaseEntity<BaseStatus>
    {
        public Guid OrderId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Description { get; set; }

        public string VerificationPassHash { get; set; } = string.Empty;

        public DateTime? ClaimedAt { get; set; }
        public Guid? ClaimedBy { get; set; }
    }
}
