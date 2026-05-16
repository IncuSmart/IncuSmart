namespace IncuSmart.Core.Utils
{
    public class PayOSOptions
    {
        public const string SectionName = "PayOS";

        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api-incusmart.io.vn";
        public string WebhookPath { get; set; } = "/api/payos/webhook";
        public string ReturnPath { get; set; } = "/api/payos/return";
        public string CancelPath { get; set; } = "/api/payos/cancel";
        public bool AutoConfirmWebhook { get; set; } = true;
        public int ExpiredAfterMinutes { get; set; } = 15;

        public string WebhookUrl => CombineUrl(WebhookPath);
        public string ReturnUrl => CombineUrl(ReturnPath);
        public string CancelUrl => CombineUrl(CancelPath);

        private string CombineUrl(string path)
        {
            var trimmedBaseUrl = (BaseUrl ?? string.Empty).TrimEnd('/');
            var normalizedPath = string.IsNullOrWhiteSpace(path) ? string.Empty : (path.StartsWith('/') ? path : $"/{path}");
            return $"{trimmedBaseUrl}{normalizedPath}";
        }
    }
}
