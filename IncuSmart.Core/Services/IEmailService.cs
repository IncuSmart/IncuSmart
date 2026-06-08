using IncuSmart.Core.Utils;

namespace IncuSmart.Core.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailDto dto);
    }
}
