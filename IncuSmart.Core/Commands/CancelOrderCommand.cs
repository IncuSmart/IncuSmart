namespace IncuSmart.Core.Commands
{
    public class CancelOrderCommand
    {
        public Guid OrderId { get; set; }
        public Guid? CancelledByUserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
