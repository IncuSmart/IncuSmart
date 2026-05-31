namespace IncuSmart.Core.Commands
{
    public class RefreshGuestPaymentLinkCommand
    {
        public string OrderCode { get; set; } = string.Empty;
        public string VerificationPass { get; set; } = string.Empty;
    }
}
