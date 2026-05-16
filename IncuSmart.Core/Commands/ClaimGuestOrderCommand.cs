namespace IncuSmart.Core.Commands
{
    public class ClaimGuestOrderCommand
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string VerificationPass { get; set; } = string.Empty;
    }
}
