using IncuSmart.Core.Services;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/test/email")]
    public class TestEmailController(IEmailService _emailService) : ApiControllerBase
    {
        [HttpPost("otp")]
        public async Task<IActionResult> SendTestOtp()
        {
            var otp = IncuSmart.Core.Utils.OtpUtil.Generate6DigitOtp();

            await _emailService.SendEmailAsync(new IncuSmart.Core.Utils.EmailDto
            {
                To = "sherrin1st@gmail.com",
                Subject = "[IncuSmart] Test OTP",
                Body = IncuSmart.Core.Utils.EmailTemplates.RegistrationOtp("Test User", otp)
            });

            return Ok(new { message = "Đã gửi email OTP thành công", otp });
        }
    }
}
