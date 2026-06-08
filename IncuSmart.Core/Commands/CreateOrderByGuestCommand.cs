namespace IncuSmart.Core.Commands
{
    public class CreateOrderByGuestCommand
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public string? Description { get; set; }
        public List<OrderItemCommand> Items { get; set; } = new();
    }

}
