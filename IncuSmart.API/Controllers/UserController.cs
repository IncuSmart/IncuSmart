using IncuSmart.Core.Domain;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController(IUserUseCase _userUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var result = await _userUseCase.Create(request.Adapt<CreateUserCommand>());
            return await FromResultAndAudit(
                new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.USER);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? role, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _userUseCase.List(role, status, page, pageSize);
            return FromResult(new BaseResponse<PagedResult<UserResponse>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data == null
                    ? null
                    : new PagedResult<UserResponse>
                    {
                        Items = result.Data.Items.Adapt<List<UserResponse>>(),
                        Page = result.Data.Page,
                        PageSize = result.Data.PageSize,
                        TotalItems = result.Data.TotalItems,
                        TotalPages = result.Data.TotalPages
                    }
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = HttpContext.GetId();
            var result = await _userUseCase.GetById(userId);
            return FromResult(new BaseResponse<UserResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data?.Adapt<UserResponse>()
            });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _userUseCase.GetById(id);
            return FromResult(new BaseResponse<UserResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data?.Adapt<UserResponse>()
            });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var command = request.Adapt<UpdateUserCommand>();
            command.Id = id;
            var result = await _userUseCase.Update(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE,
                AuditEntityType.USER,
                id);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        {
            var command = request.Adapt<UpdateProfileCommand>();
            command.Id = HttpContext.GetId();
            var result = await _userUseCase.UpdateProfile(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE_PROFILE,
                AuditEntityType.USER,
                command.Id);
        }

        [Authorize]
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            request.UserId = HttpContext.GetId();
            var result = await _userUseCase.ChangePassword(request.Adapt<ChangePasswordCommand>());
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CHANGE_PASSWORD,
                AuditEntityType.USER,
                request.UserId);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/password/reset")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
        {
            var command = request.Adapt<ResetPasswordCommand>();
            command.UserId = id;
            var result = await _userUseCase.ResetPassword(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.RESET_PASSWORD,
                AuditEntityType.USER,
                id);
        }
    }
}
