namespace IncuSmart.Core.Commands
{
    public class HandleOrderPaymentWebhookCommand
    {
        public long PaymentOrderCode { get; set; }
        public long Amount { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? Reference { get; set; }
        public string? TransactionDateTime { get; set; }
        public string? ProviderCode { get; set; }
        public string? ProviderDescription { get; set; }
        public bool Success { get; set; }
    }
}
