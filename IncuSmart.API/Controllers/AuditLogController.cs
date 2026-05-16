using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    public class AuditLogController(IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? userId,
            [FromQuery] string? action,
            [FromQuery] string? entity,
            [FromQuery] PagingRequest paging)
        {
            var result = await _auditLogUseCase.List(userId, action, entity, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<AuditLog>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _auditLogUseCase.GetById(id);
            return FromResult(new BaseResponse<AuditLog?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }
    }
}
