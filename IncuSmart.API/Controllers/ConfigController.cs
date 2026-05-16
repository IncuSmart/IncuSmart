using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/configs")]
    public class ConfigController(IConfigUseCase _configUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var result = await _configUseCase.Create(request.Adapt<CreateConfigCommand>());
            return await FromResultAndAudit(
                new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.CONFIG);
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,TECHNICIAN,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? type, [FromQuery] string? status, [FromQuery] PagingRequest paging)
        {
            var result = await _configUseCase.List(type, status, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<Config>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _configUseCase.GetById(id);
            return FromResult(new BaseResponse<Config?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConfigRequest request)
        {
            var command = request.Adapt<UpdateConfigCommand>();
            command.Id = id;
            var result = await _configUseCase.Update(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE,
                AuditEntityType.CONFIG,
                id);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _configUseCase.Delete(id);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.DELETE,
                AuditEntityType.CONFIG,
                id);
        }
    }
}
