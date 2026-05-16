using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertController(IAlertUseCase _alertUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _alertUseCase.GetById(id, HttpContext.GetId(), HttpContext.GetRole());
            return FromResult(new BaseResponse<Alert?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid? incubatorId, [FromQuery] string? severity, [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PagingRequest paging)
        {
            var result = await _alertUseCase.List(incubatorId, severity, status, from, to, HttpContext.GetId(), HttpContext.GetRole(), paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<Alert>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveAlertRequest request)
        {
            var command = request.Adapt<ResolveAlertCommand>();
            command.Id = id;
            command.ResolvedBy = AlertResolvedBy.MANUAL;
            command.UpdatedBy = HttpContext.GetId() != Guid.Empty ? HttpContext.GetId().ToString() : CommonConst.SystemActor;
            var result = await _alertUseCase.Resolve(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.RESOLVE,
                AuditEntityType.ALERT,
                id);
        }
    }
}
