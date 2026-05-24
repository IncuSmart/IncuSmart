using System.Text.Json.Serialization;

namespace IncuSmart.API.Requests
{
    public class PayOSWebhookRequest
    {
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;
        public bool Success { get; set; }
        public PayOSWebhookDataRequest Data { get; set; } = new();
        public string Signature { get; set; } = string.Empty;
    }

    public class PayOSWebhookDataRequest
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
        [JsonPropertyName("desc")]
        public string? Description2 { get; set; }
        public string? CounterAccountBankId { get; set; }
        public string? CounterAccountBankName { get; set; }
        public string? CounterAccountName { get; set; }
        public string? CounterAccountNumber { get; set; }
        public string? VirtualAccountName { get; set; }
        public string? VirtualAccountNumber { get; set; }
    }
}
