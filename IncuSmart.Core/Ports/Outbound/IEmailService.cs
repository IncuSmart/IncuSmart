namespace IncuSmart.Core.Ports.Outbound
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            string? plainText = null,
            string? fromName = null
        );
    }
}
