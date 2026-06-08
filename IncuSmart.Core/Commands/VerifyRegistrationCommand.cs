namespace IncuSmart.Core.Commands
{
    public class VerifyRegistrationCommand
    {
        public string SessionId { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
