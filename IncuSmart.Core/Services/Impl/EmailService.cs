using IncuSmart.Core.Utils;
using System.Net;
using System.Net.Mail;

namespace IncuSmart.Core.Services.Impl
{
    public class EmailService : IEmailService
    {
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;

        private readonly EmailProperties _props;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailProperties> options, ILogger<EmailService> logger)
        {
            _props = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailDto dto)
        {
            _logger.LogInformation("Gửi email đến {To}", dto.To);

            using var client = new SmtpClient(SmtpHost, SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_props.Username, _props.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_props.Username, _props.DisplayName),
                Subject = dto.Subject,
                Body = dto.Body,
                IsBodyHtml = dto.IsHtml
            };
            message.To.Add(dto.To);

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("Gửi email thành công đến {To}", dto.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email thất bại đến {To}", dto.To);
                throw;
            }
        }
    }
}
