using System.Text.Json;

namespace IncuSmart.Core.Utils
{
    public class SMSUtils
    {
        private const string EsmsEndpoint =
            "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";

        private static readonly SMSProperties _smsProperties = new()
        {
            ApiKey = Environment.GetEnvironmentVariable("ESMS_API_KEY") ?? string.Empty,
            SecretKey = Environment.GetEnvironmentVariable("ESMS_SECRET_KEY") ?? string.Empty,
        };

        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Gửi SMS thông qua eSMS API (SendMultipleMessage_V4).
        /// </summary>
        /// <param name="dto">Thông tin tin nhắn cần gửi</param>
        /// <returns>SMSResponse trả về từ server</returns>
        /// <exception cref="HttpRequestException">Request thất bại</exception>
        /// <exception cref="InvalidOperationException">Server trả về lỗi</exception>
        public static async Task<SMSResponse> SendSMSAsync(SMSDto dto)
        {
            var payload = new Dictionary<string, string>
            {
                ["ApiKey"] = _smsProperties.ApiKey,
                ["SecretKey"] = _smsProperties.SecretKey,
                ["Phone"] = dto.Phone,
                ["Content"] = dto.Content,
                ["Brandname"] = dto.Brandname,
                ["SmsType"] = dto.SmsType,
                ["IsUnicode"] = dto.IsUnicode,
            };

            if (!string.IsNullOrEmpty(dto.CampaignId)) payload["campaignid"] = dto.CampaignId;
            if (!string.IsNullOrEmpty(dto.RequestId)) payload["RequestId"] = dto.RequestId;
            if (!string.IsNullOrEmpty(dto.CallbackUrl)) payload["CallbackUrl"] = dto.CallbackUrl;

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(EsmsEndpoint, content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<SMSResponse>(body)
                       ?? throw new InvalidOperationException("eSMS: response deserialization failed.");

            if (data.CodeResult != "100")
                throw new InvalidOperationException($"eSMS error: CodeResult={data.CodeResult}");

            return data;
        }
    }

}
