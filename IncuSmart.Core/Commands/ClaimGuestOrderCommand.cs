namespace IncuSmart.Core.Commands
{
    public class ClaimGuestOrderCommand
    {
        public string OrderCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string VerificationPass { get; set; } = string.Empty;
    }
}
