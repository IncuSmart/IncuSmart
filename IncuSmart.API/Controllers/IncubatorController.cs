using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/incubators")]
    public class IncubatorController(IIncubatorUseCase _incubatorUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateIncubatorRequest request)
        {
            var result = await _incubatorUseCase.Create(request.Adapt<CreateIncubatorCommand>());
            return await FromResultAndAudit(
                new BaseResponse<List<Guid>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.INCUBATOR);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] Guid? modelId,
            [FromQuery] PagingRequest paging)
        {
            var result = await _incubatorUseCase.List(HttpContext.GetId(), HttpContext.GetRole(), status, modelId, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<IncubatorResponse>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _incubatorUseCase.GetById(id, HttpContext.GetId(), HttpContext.GetRole());
            return FromResult(new BaseResponse<IncubatorResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPut("{id}/config-instances")]
        public async Task<IActionResult> UpdateConfigInstances(Guid id, [FromBody] UpdateConfigInstancesRequest request)
        {
            var command = new UpdateConfigInstancesCommand
            {
                IncubatorId = id,
                Items = request.Items.Adapt<List<UpdateConfigInstanceItemCommand>>()
            };
            var result = await _incubatorUseCase.UpdateConfigInstances(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE_CONFIG,
                AuditEntityType.INCUBATOR,
                id);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateIncubatorStatusRequest request)
        {
            var result = await _incubatorUseCase.UpdateStatus(id, request.Status, HttpContext.GetId().ToString());
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE_STATUS,
                AuditEntityType.INCUBATOR,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}/hatching-seasons")]
        public async Task<IActionResult> GetHatchingSeasons(Guid id, [FromQuery] string? status, [FromQuery] string? eggType)
        {
            var result = await _incubatorUseCase.GetHatchingSeasons(id, HttpContext.GetId(), HttpContext.GetRole(), status, eggType);
            return FromResult(new BaseResponse<List<HatchingSeason>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
