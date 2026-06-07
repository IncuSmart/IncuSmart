using IncuSmart.Core.Ports.Outbound;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/email")]
    public class EmailController(IEmailService _emailService) : ApiControllerBase
    {
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendEmailRequest request)
        {
            await _emailService.SendEmailAsync(
                request.To,
                request.Subject,
                request.HtmlBody,
                request.PlainText);

            return Ok(new BaseResponse<bool>
            {
                StatusCode = "200",
                Message = "Email sent successfully",
                Data = true
            });
        }
    }
}
