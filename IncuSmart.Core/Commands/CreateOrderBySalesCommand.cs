namespace IncuSmart.Core.Commands
{
    public class CreateOrderBySalesCommand
    {
        public Guid CustomerId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public List<OrderItemCommand> Items { get; set; } = new();
    }
}
