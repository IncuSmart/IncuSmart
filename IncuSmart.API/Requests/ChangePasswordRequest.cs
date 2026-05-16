using System.Text.Json.Serialization;

namespace IncuSmart.API.Requests
{
    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(6, ErrorMessage = "Current password must be at least 6 characters")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}
