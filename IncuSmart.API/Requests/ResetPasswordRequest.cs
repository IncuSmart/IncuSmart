namespace IncuSmart.API.Requests
{
    public class ResetPasswordRequest
    {
        [Required]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
