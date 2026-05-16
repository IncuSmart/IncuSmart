using IncuSmart.Core;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthUseCase _authUseCase, IConfiguration _configuration) : ApiControllerBase
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

        [HttpPost("admin/login")]
        public IActionResult AdminLogin([FromBody] AdminLoginRequest request)
        {
            var username = _configuration["AdminAuth:Username"] ?? "admin";
            var password = _configuration["AdminAuth:Password"] ?? "admin";

            if (request.Username != username || request.Password != password)
            {
                return FromResult(new BaseResponse<string>
                {
                    StatusCode = "404",
                    Message = CommonConst.WrongUsernameOrPassword,
                    Data = null
                });
            }

            var token = JwtUtil.GenerateToken(new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = username,
                FullName = "Administrator",
                PasswordHash = string.Empty,
                Phone = string.Empty,
                Role = UserRole.ADMIN,
                Status = BaseStatus.ACTIVE
            });

            return FromResult(new BaseResponse<string>
            {
                StatusCode = "200",
                Message = CommonConst.LoginSuccessfully,
                Data = token
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
