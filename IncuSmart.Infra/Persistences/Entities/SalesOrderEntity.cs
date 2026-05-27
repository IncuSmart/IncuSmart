namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("sales_orders")]
    public class SalesOrderEntity : BaseEntity<OrderStatus>
    {
        public string? OrderCode { get; set; }

        public Guid? CustomerId { get; set; }

        public DateTime? OrderDate { get; set; }

        public string ShippingAddress { get; set; } = string.Empty;

        public long TotalAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public long? PaymentOrderCode { get; set; }

        public string? PaymentLinkId { get; set; }

        public string? QrCode { get; set; }

        public DateTime? PaymentLinkCreatedAt { get; set; }

        public DateTime? PaymentLinkExpiredAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
