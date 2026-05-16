using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/maintenance-tickets")]
    public class MaintenanceTicketController(IMaintenanceTicketUseCase _maintenanceTicketUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMaintenanceTicketRequest request)
        {
            var result = await _maintenanceTicketUseCase.Create(request.Adapt<CreateMaintenanceTicketCommand>(), HttpContext.GetId(), HttpContext.GetRole());
            return await FromResultAndAudit(
                new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.MAINTENANCE_TICKET);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _maintenanceTicketUseCase.GetById(id, HttpContext.GetId(), HttpContext.GetRole());
            return FromResult(new BaseResponse<MaintenanceTicketDetailResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid? incubatorId, [FromQuery] Guid? technicianId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _maintenanceTicketUseCase.List(incubatorId, technicianId, status, HttpContext.GetId(), HttpContext.GetRole(), page, pageSize);
            return FromResult(new BaseResponse<PagedResult<MaintenanceTicket>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPut("{id}/assign")]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignMaintenanceTicketRequest request)
        {
            var command = request.Adapt<AssignMaintenanceTicketCommand>();
            command.Id = id;
            var result = await _maintenanceTicketUseCase.Assign(command, HttpContext.GetId(), HttpContext.GetRole());
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.ASSIGN,
                AuditEntityType.MAINTENANCE_TICKET,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMaintenanceTicketStatusRequest request)
        {
            var command = request.Adapt<UpdateMaintenanceTicketStatusCommand>();
            command.Id = id;
            var result = await _maintenanceTicketUseCase.UpdateStatus(command, HttpContext.GetId(), HttpContext.GetRole());
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.UPDATE_STATUS,
                AuditEntityType.MAINTENANCE_TICKET,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var result = await _maintenanceTicketUseCase.Cancel(id, HttpContext.GetId(), HttpContext.GetRole());
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CANCEL,
                AuditEntityType.MAINTENANCE_TICKET,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("{id}/logs")]
        public async Task<IActionResult> AddLog(Guid id, [FromBody] CreateMaintenanceLogRequest request)
        {
            var command = request.Adapt<CreateMaintenanceLogCommand>();
            command.TicketId = id;
            var result = await _maintenanceTicketUseCase.AddLog(command, HttpContext.GetId(), HttpContext.GetRole());
            return await FromResultAndAudit(
                new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.ADD_LOG,
                AuditEntityType.MAINTENANCE_TICKET,
                id);
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}/logs")]
        public async Task<IActionResult> GetLogs(Guid id)
        {
            var result = await _maintenanceTicketUseCase.GetLogs(id, HttpContext.GetId(), HttpContext.GetRole());
            return FromResult(new BaseResponse<List<MaintenanceLog>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
