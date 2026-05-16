namespace IncuSmart.Core.Ports.Outbound
{
    public interface IPaymentGatewayService
    {
        Task<PaymentLinkResult> CreatePaymentLink(PaymentLinkRequest request);
        Task ConfirmWebhook(string webhookUrl);
        Task<PaymentWebhookResult> VerifyWebhook(PaymentWebhookRequest request);
    }

    public class PaymentLinkRequest
    {
        public long OrderCode { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }
        public string? BuyerAddress { get; set; }
        public List<PaymentItemRequest> Items { get; set; } = [];
    }

    public class PaymentItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public long Price { get; set; }
    }

    public class PaymentLinkResult
    {
        public long OrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? CheckoutUrl { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }

    public class PaymentWebhookRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Success { get; set; }
        public PaymentWebhookDataRequest Data { get; set; } = new();
        public string Signature { get; set; } = string.Empty;
    }

    public class PaymentWebhookDataRequest
    {
        public long OrderCode { get; set; }
        public long Amount { get; set; }
        public string? Description { get; set; }
        public string? AccountNumber { get; set; }
        public string? Reference { get; set; }
        public string? TransactionDateTime { get; set; }
        public string? Currency { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? Code { get; set; }
        public string? Description2 { get; set; }
        public string? CounterAccountBankId { get; set; }
        public string? CounterAccountBankName { get; set; }
        public string? CounterAccountName { get; set; }
        public string? CounterAccountNumber { get; set; }
        public string? VirtualAccountName { get; set; }
        public string? VirtualAccountNumber { get; set; }
    }

    public class PaymentWebhookResult
    {
        public long OrderCode { get; set; }
        public long Amount { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? Reference { get; set; }
        public string? TransactionDateTime { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool Success { get; set; }
    }
}
