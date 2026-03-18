namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("sales_orders")]
    public class SalesOrderEntity : BaseEntity<BaseStatus>
    {
        public string? OrderCode { get; set; }

        public Guid? CustomerId { get; set; }

        public DateTime? OrderDate { get; set; }

        public string ShippingAddress { get; set; } = string.Empty;
    }
}
