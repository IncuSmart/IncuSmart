namespace IncuSmart.Core.Commands
{
    public class AssignIncubatorToOrderItemCommand
    {
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid IncubatorId { get; set; }
    }
}
