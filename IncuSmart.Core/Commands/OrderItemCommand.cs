namespace IncuSmart.Core.Commands
{
    public class OrderItemCommand
    {
        public Guid IncubatorModelId { get; set; }
        public int Quantity { get; set; }
    }
}
