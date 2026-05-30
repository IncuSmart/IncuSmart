using System.Text.Json.Serialization;

namespace IncuSmart.API.Requests
{
    public class ClaimGuestOrderRequest
    {
        [Required]
        public string OrderCode { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Verification pass must be at least 6 characters")]
        public string VerificationPass { get; set; } = string.Empty;

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}
