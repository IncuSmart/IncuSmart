namespace IncuSmart.Core.Commands
{
    public class CompleteOrderCommand
    {
        public Guid OrderId { get; set; }
        public Guid? UserId { get; set; }
        public string? Role { get; set; }
    }
}
