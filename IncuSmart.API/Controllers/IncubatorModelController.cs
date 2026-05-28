using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/incubator-models")]
    public class IncubatorModelController(IIncubatorModelUseCase _modelUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateIncubatorModelRequest request)
        {
            var result = await _modelUseCase.Create(request.Adapt<CreateIncubatorModelCommand>());
            return await FromResultAndAudit(
                new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.INCUBATOR_MODEL);
        }

        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<IActionResult> ListPublic([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var result = await _modelUseCase.List("ACTIVE", search, page, pageSize);
            return FromResult(new BaseResponse<PagedResult<IncubatorModel>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,TECHNICIAN,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _modelUseCase.List(status, search, page, pageSize);
            return FromResult(new BaseResponse<PagedResult<IncubatorModel>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _modelUseCase.GetById(id);
            return FromResult(new BaseResponse<IncubatorModel?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpGet("{id}/configs")]
        public async Task<IActionResult> GetConfigs(Guid id)
        {
            var result = await _modelUseCase.GetConfigs(id);
            return FromResult(new BaseResponse<List<ModelConfigWithDetail>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncubatorModelRequest request)
        {
            var command = request.Adapt<UpdateIncubatorModelCommand>();
            command.Id = id;
            var result = await _modelUseCase.Update(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE,
                AuditEntityType.INCUBATOR_MODEL,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _modelUseCase.Delete(id);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.DELETE,
                AuditEntityType.INCUBATOR_MODEL,
                id);
        }
    }
}
