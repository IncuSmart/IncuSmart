using Net.payOS;
using Net.payOS.Types;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CorePayOSOptions = IncuSmart.Core.Utils.PayOSOptions;

namespace IncuSmart.Infra.Services
{
    public class PayOSPaymentGatewayService : IPaymentGatewayService
    {
        private readonly CorePayOSOptions _options;
        private readonly ILogger<PayOSPaymentGatewayService> _logger;
        private readonly PayOS _payOS;
        private static readonly HttpClient _http = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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

            _payOS = new PayOS(_options.ClientId, _options.ApiKey, _options.ChecksumKey);
        }

        // ──────────────────────────────────────────────────────────────────
        // CreatePaymentLink — dùng HttpClient trực tiếp (bypass SDK)
        // Signature spec: HMAC-SHA256 over sorted 5 fields:
        //   amount, cancelUrl, description, orderCode, returnUrl
        // ──────────────────────────────────────────────────────────────────
        public async Task<PaymentLinkResult> CreatePaymentLink(PaymentLinkRequest request)
        {
            var expiredAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiredAfterMinutes);
            var amount = request.Amount > int.MaxValue
                ? throw new InvalidOperationException(CommonConst.OrderAmountExceedsPaymentGatewayLimit)
                : (int)request.Amount;

            var signature = ComputePaymentSignature(
                request.OrderCode, amount, request.Description,
                _options.ReturnUrl, _options.CancelUrl);

            var payload = new PayOSCreateRequest
            {
                OrderCode   = request.OrderCode,
                Amount      = amount,
                Description = request.Description,
                ReturnUrl   = _options.ReturnUrl,
                CancelUrl   = _options.CancelUrl,
                BuyerName   = request.BuyerName,
                BuyerEmail  = request.BuyerEmail,
                BuyerPhone  = request.BuyerPhone,
                BuyerAddress = request.BuyerAddress,
                ExpiredAt   = (int)expiredAt.ToUnixTimeSeconds(),
                Items = request.Items.Select(x => new PayOSItemRequest
                {
                    Name     = x.Name,
                    Quantity = x.Quantity,
                    Price    = checked((int)x.Price)
                }).ToList(),
                Signature = signature
            };

            var bodyJson = JsonSerializer.Serialize(payload, _jsonOpts);
            _logger.LogInformation("PayOS create request body: {Body}", bodyJson);

            using var httpReq = new HttpRequestMessage(HttpMethod.Post,
                "https://api-merchant.payos.vn/v2/payment-requests");
            httpReq.Headers.Add("x-client-id", _options.ClientId);
            httpReq.Headers.Add("x-api-key",   _options.ApiKey);
            httpReq.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            var httpResp = await _http.SendAsync(httpReq);
            var json = await httpResp.Content.ReadAsStringAsync();

            _logger.LogInformation("PayOS create response [{Status}]: {Json}", (int)httpResp.StatusCode, json);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var code = root.GetProperty("code").GetString();

            if (code != "00")
            {
                var desc = root.TryGetProperty("desc", out var d) ? d.GetString() : json;
                throw new InvalidOperationException($"PayOS error {code}: {desc}");
            }

            var data = root.GetProperty("data");
            var paymentLinkId = data.GetProperty("paymentLinkId").GetString()!;
            var qrCode        = data.TryGetProperty("qrCode", out var qr) ? qr.GetString() : null;

            return new PaymentLinkResult
            {
                OrderCode     = request.OrderCode,
                PaymentLinkId = paymentLinkId,
                QrCode        = qrCode,
                ExpiredAt     = expiredAt.UtcDateTime
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // ConfirmWebhook
        // ──────────────────────────────────────────────────────────────────
        public async Task ConfirmWebhook(string webhookUrl)
        {
            await _payOS.confirmWebhook(webhookUrl);
        }

        // ──────────────────────────────────────────────────────────────────
        // VerifyWebhook — dùng SDK v1.0.9
        // ──────────────────────────────────────────────────────────────────
        public Task<PaymentWebhookResult> VerifyWebhook(PaymentWebhookRequest request)
        {
            var webhookData = new WebhookData(
                orderCode:            request.Data.OrderCode,
                amount:               checked((int)request.Data.Amount),
                description:          request.Data.Description ?? string.Empty,
                accountNumber:        request.Data.AccountNumber ?? string.Empty,
                reference:            request.Data.Reference ?? string.Empty,
                transactionDateTime:  request.Data.TransactionDateTime ?? string.Empty,
                currency:             request.Data.Currency ?? string.Empty,
                paymentLinkId:        request.Data.PaymentLinkId ?? string.Empty,
                code:                 request.Data.Code ?? string.Empty,
                desc:                 request.Data.Description2 ?? string.Empty,
                counterAccountBankId:   request.Data.CounterAccountBankId,
                counterAccountBankName: request.Data.CounterAccountBankName,
                counterAccountName:     request.Data.CounterAccountName,
                counterAccountNumber:   request.Data.CounterAccountNumber,
                virtualAccountName:     request.Data.VirtualAccountName,
                virtualAccountNumber:   request.Data.VirtualAccountNumber ?? string.Empty
            );

            var webhookType = new WebhookType(
                code:      request.Code,
                desc:      request.Description ?? string.Empty,
                success:   request.Success,
                data:      webhookData,
                signature: request.Signature
            );

            _payOS.verifyPaymentWebhookData(webhookType);   // throws nếu signature sai

            return Task.FromResult(new PaymentWebhookResult
            {
                OrderCode           = webhookData.orderCode,
                Amount              = webhookData.amount,
                PaymentLinkId       = webhookData.paymentLinkId,
                Reference           = webhookData.reference,
                TransactionDateTime = webhookData.transactionDateTime,
                Code                = webhookData.code,
                Description         = webhookData.desc,
                Success             = request.Success
            });
        }

        // ──────────────────────────────────────────────────────────────────
        // Helper: compute HMAC-SHA256 over the 5 required fields
        // ──────────────────────────────────────────────────────────────────
        private string ComputePaymentSignature(
            long orderCode, int amount, string description,
            string returnUrl, string cancelUrl)
        {
            // PayOS spec: sort alphabetically, format "key=value&..."
            var raw = $"amount={amount}" +
                      $"&cancelUrl={cancelUrl}" +
                      $"&description={description}" +
                      $"&orderCode={orderCode}" +
                      $"&returnUrl={returnUrl}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ChecksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // ──────────────────────────────────────────────────────────────────
        // Internal DTOs cho HTTP request
        // ──────────────────────────────────────────────────────────────────
        private sealed class PayOSCreateRequest
        {
            public long   OrderCode   { get; set; }
            public int    Amount      { get; set; }
            public string Description { get; set; } = string.Empty;
            public string ReturnUrl   { get; set; } = string.Empty;
            public string CancelUrl   { get; set; } = string.Empty;
            public string Signature   { get; set; } = string.Empty;
            public string? BuyerName    { get; set; }
            public string? BuyerEmail   { get; set; }
            public string? BuyerPhone   { get; set; }
            public string? BuyerAddress { get; set; }
            public int?    ExpiredAt    { get; set; }
            public List<PayOSItemRequest> Items { get; set; } = [];
        }

        private sealed class PayOSItemRequest
        {
            public string Name     { get; set; } = string.Empty;
            public int    Quantity { get; set; }
            public int    Price    { get; set; }
        }
    }
}
