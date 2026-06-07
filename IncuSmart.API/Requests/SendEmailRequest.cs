using System.ComponentModel.DataAnnotations;

namespace IncuSmart.API.Requests
{
    public class SendEmailRequest
    {
        [Required]
        [EmailAddress]
        public string To { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string HtmlBody { get; set; } = string.Empty;

        public string? PlainText { get; set; }
    }
}
