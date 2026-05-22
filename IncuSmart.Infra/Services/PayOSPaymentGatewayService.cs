using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using CorePayOSOptions = IncuSmart.Core.Utils.PayOSOptions;

namespace IncuSmart.Infra.Services
{
    public class PayOSPaymentGatewayService : IPaymentGatewayService
    {
        private readonly CorePayOSOptions _options;
        private readonly ILogger<PayOSPaymentGatewayService> _logger;
        private readonly PayOSClient _client;

        public PayOSPaymentGatewayService(IOptions<CorePayOSOptions> options, ILogger<PayOSPaymentGatewayService> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.ClientId)
                || string.IsNullOrWhiteSpace(_options.ApiKey)
                || string.IsNullOrWhiteSpace(_options.ChecksumKey))
            {
                throw new InvalidOperationException(CommonConst.PaymentLinkMissingConfiguration);
            }

            _client = new PayOSClient(_options.ClientId, _options.ApiKey, _options.ChecksumKey);
        }

        public async Task<PaymentLinkResult> CreatePaymentLink(PaymentLinkRequest request)
        {
            var expiredAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiredAfterMinutes);
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = request.OrderCode,
                Amount = request.Amount > int.MaxValue
                    ? throw new InvalidOperationException(CommonConst.OrderAmountExceedsPaymentGatewayLimit)
                    : (int)request.Amount,
                Description = request.Description,
                ReturnUrl = _options.ReturnUrl,
                CancelUrl = _options.CancelUrl,
                BuyerName = request.BuyerName,
                BuyerEmail = request.BuyerEmail,
                BuyerPhone = request.BuyerPhone,
                BuyerAddress = request.BuyerAddress,
                ExpiredAt = expiredAt.ToUnixTimeSeconds(),
                Items = request.Items.Select(x => new PaymentLinkItem
                {
                    Name = x.Name,
                    Quantity = x.Quantity,
                    Price = checked((int)x.Price)
                }).ToList()
            };

            var paymentLink = await _client.PaymentRequests.CreateAsync(paymentRequest);
            return new PaymentLinkResult
            {
                OrderCode = request.OrderCode,
                PaymentLinkId = paymentLink.PaymentLinkId,
                CheckoutUrl = paymentLink.CheckoutUrl,
                ExpiredAt = expiredAt.UtcDateTime
            };
        }

        public async Task ConfirmWebhook(string webhookUrl)
        {
            await _client.Webhooks.ConfirmAsync(webhookUrl);
        }

        public async Task<PaymentWebhookResult> VerifyWebhook(PaymentWebhookRequest request)
        {
            var verifiedData = await _client.Webhooks.VerifyAsync(new Webhook
            {
                Code = request.Code,
                Description = request.Description,
                Success = request.Success,
                Signature = request.Signature,
                Data = new WebhookData
                {
                    OrderCode = request.Data.OrderCode,
                    Amount = checked((int)request.Data.Amount),
                    Description = request.Data.Description ?? string.Empty,
                    AccountNumber = request.Data.AccountNumber ?? string.Empty,
                    Reference = request.Data.Reference ?? string.Empty,
                    TransactionDateTime = request.Data.TransactionDateTime ?? string.Empty,
                    Currency = request.Data.Currency ?? string.Empty,
                    PaymentLinkId = request.Data.PaymentLinkId ?? string.Empty,
                    Code = request.Data.Code ?? string.Empty,
                    Description2 = request.Data.Description2 ?? string.Empty,
                    CounterAccountBankId = request.Data.CounterAccountBankId ?? string.Empty,
                    CounterAccountBankName = request.Data.CounterAccountBankName ?? string.Empty,
                    CounterAccountName = request.Data.CounterAccountName ?? string.Empty,
                    CounterAccountNumber = request.Data.CounterAccountNumber ?? string.Empty,
                    VirtualAccountName = request.Data.VirtualAccountName ?? string.Empty,
                    VirtualAccountNumber = request.Data.VirtualAccountNumber ?? string.Empty
                }
            });

            return new PaymentWebhookResult
            {
                OrderCode = verifiedData.OrderCode,
                Amount = verifiedData.Amount,
                PaymentLinkId = verifiedData.PaymentLinkId,
                Reference = verifiedData.Reference,
                TransactionDateTime = verifiedData.TransactionDateTime,
                Code = verifiedData.Code,
                Description = verifiedData.Description2,
                Success = request.Success
            };
        }
    }
}
