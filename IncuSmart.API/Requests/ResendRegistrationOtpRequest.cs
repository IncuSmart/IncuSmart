namespace IncuSmart.API.Requests
{
    public class ResendRegistrationOtpRequest
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;
    }
}
