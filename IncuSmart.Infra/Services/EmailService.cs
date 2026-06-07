using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Text.RegularExpressions;

namespace IncuSmart.Infra.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string? plainText = null,
        string? fromName = null)
    {
        if (string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Email configuration is not set.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            fromName ?? _options.FromName,
            _options.FromAddress));

        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainText ?? HtmlToPlainText(htmlBody)
        };

        message.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.SmtpHost,
                _options.SmtpPort,
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_options.Username, _options.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailTo}", to);
            throw;
        }
    }

    private static string HtmlToPlainText(string html)
    {
        return Regex.Replace(html, "<.*?>", string.Empty);
    }
}
