namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("customers")]
    public class CustomerEntity : BaseEntity<BaseStatus>
    {
        public Guid UserId { get; set; }

        public string? DeviceToken { get; set; }

        public string? PinNumberHash { get; set; }

        public string? Address { get; set; }

        [NotMapped]
        public List<SalesOrderEntity> SalesOrders { get; set; } = [];
    }
}
