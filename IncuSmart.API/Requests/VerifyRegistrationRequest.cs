namespace IncuSmart.API.Requests
{
    public class VerifyRegistrationRequest
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải đúng 6 chữ số")]
        public string Otp { get; set; } = string.Empty;
    }
}
