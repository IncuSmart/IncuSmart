using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    public class WarrantyController(IWarrantyUseCase _warrantyUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,SALES_STAFF")]
        [HttpPost("api/warranties")]
        public async Task<IActionResult> Create([FromBody] CreateWarrantyRequest request)
        {
            var result = await _warrantyUseCase.Create(request.Adapt<CreateWarrantyCommand>());
            return await FromResultAndAudit(
                new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.WARRANTY);
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF")]
        [HttpPut("api/warranties/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarrantyRequest request)
        {
            var command = request.Adapt<UpdateWarrantyCommand>();
            command.Id = id;
            var result = await _warrantyUseCase.Update(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE_WARRANTY,
                AuditEntityType.WARRANTY,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("api/incubators/{incubatorId}/warranty")]
        public async Task<IActionResult> GetByIncubatorId(Guid incubatorId)
        {
            var result = await _warrantyUseCase.GetByIncubatorId(incubatorId, HttpContext.GetId(), HttpContext.GetRole());
            return FromResult(new BaseResponse<Warranty?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
