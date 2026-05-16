using IncuSmart.Core.Enums;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("sales_order_items")]
    public class SalesOrderItemEntity : BaseEntity<OrderItemStatus>
    {
        public Guid OrderId { get; set; }
        public Guid IncubatorModelId { get; set; }
        public Guid? IncubatorId { get; set; }
        public long UnitPrice { get; set; }
    }
}
