using System.Text.Json;

namespace IncuSmart.Core.Services.Impl
{
    public class SMSService : ISMSService
    {
        private readonly SMSProperties _smsProperties;
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<SMSService> _logger;

        public SMSService(IOptions<SMSProperties> options, ILogger<SMSService> logger)
        {
            _smsProperties = options.Value;
            _logger = logger;
        }

        public async Task<SMSResponse> SendSMSAsync(SMSDto dto)
        {
            _logger.LogInformation("Gửi SMS đến {Phone}", dto.Phone);

            var payload = new Dictionary<string, string>
            {
                ["ApiKey"] = _smsProperties.ApiKey,
                ["SecretKey"] = _smsProperties.SecretKey,
                ["Phone"] = dto.Phone,
                ["Content"] = dto.Content,
                ["SmsType"] = dto.SmsType,
                ["IsUnicode"] = dto.IsUnicode,
            };

            if (!string.IsNullOrEmpty(dto.Brandname)) payload["Brandname"] = dto.Brandname;
            if (!string.IsNullOrEmpty(dto.CampaignId)) payload["campaignid"] = dto.CampaignId;
            if (!string.IsNullOrEmpty(dto.RequestId)) payload["RequestId"] = dto.RequestId;
            if (!string.IsNullOrEmpty(dto.CallbackUrl)) payload["CallbackUrl"] = dto.CallbackUrl;

            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_smsProperties.Endpoint, content);

                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<SMSResponse>(body)
                           ?? throw new InvalidOperationException("eSMS: không deserialize được response.");

                if (data.CodeResult != "100")
                {
                    _logger.LogError(
                        "Gửi SMS thất bại đến {Phone} - CodeResult: {CodeResult} - SMSID: {SMSID} - RawBody: {Body}",
                        dto.Phone, data.CodeResult, data.SMSID, body
                    );
                    throw new InvalidOperationException($"eSMS lỗi CodeResult={data.CodeResult}");
                }

                _logger.LogInformation("Gửi SMS thành công đến {Phone}, SMSID={SMSID}", dto.Phone, data.SMSID);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi SMS thất bại đến {Phone}", dto.Phone);
                throw;
            }
        }
    }
}
