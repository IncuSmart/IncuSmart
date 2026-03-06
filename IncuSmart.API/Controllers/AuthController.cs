using IncuSmart.Core;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthUseCase _authUseCase) : ApiControllerBase
    {

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            ResultModel<string?> result = await _authUseCase.Login(request.Adapt<LoginCommand>());

            return FromResult(new BaseResponse<string>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            ResultModel<string?> result = await _authUseCase.Register(request.Adapt<RegisterCommand>());

            return FromResult(new BaseResponse<string> 
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }
    }
}
