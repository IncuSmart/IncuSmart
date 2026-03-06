namespace IncuSmart.Core.Domains
{
    public class SalesOrder : BaseDomain<OrderStatus>
    {
        public string? OrderCode { get; set; }

        public Guid? CustomerId { get; set; }

        public DateTime? OrderDate { get; set; }
    }
}
