using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/maintenance-tickets")]
    public class MaintenanceTicketController(
        IMaintenanceTicketUseCase _maintenanceTicketUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/maintenance-tickets
        /// Tạo phiếu bảo trì — ADMIN, TECHNICIAN
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMaintenanceTicketRequest request)
        {
            var result = await _maintenanceTicketUseCase.Create(
                request.Adapt<CreateMaintenanceTicketCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/maintenance-tickets/{id}
        /// Xem chi tiết phiếu bảo trì kèm logs — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ xem được ticket của máy mình
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _maintenanceTicketUseCase.GetById(id, userId, role);
            return FromResult(new BaseResponse<MaintenanceTicketDetailResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Adapt<MaintenanceTicketDetailResponse>() });
        }

        /// <summary>
        /// GET /api/maintenance-tickets
        /// Xem danh sách phiếu bảo trì — ADMIN, TECHNICIAN, CUSTOMER
        /// Lọc: incubatorId?, technicianId?, status?
        /// CUSTOMER: chỉ thấy ticket của máy mình
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid?   incubatorId,
            [FromQuery] Guid?   technicianId,
            [FromQuery] string? status)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _maintenanceTicketUseCase.GetAll(
                incubatorId, technicianId, status, userId, role);
            return FromResult(new BaseResponse<List<MaintenanceTicket>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/maintenance-tickets/{id}/status
        /// Cập nhật trạng thái phiếu bảo trì — ADMIN, TECHNICIAN
        /// Đóng ticket → tự động set ClosedAt
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            Guid id, [FromBody] UpdateMaintenanceTicketStatusRequest request)
        {
            var command = request.Adapt<UpdateMaintenanceTicketStatusCommand>();
            command.Id  = id;
            var result  = await _maintenanceTicketUseCase.UpdateStatus(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// POST /api/maintenance-tickets/{id}/logs
        /// Tạo nhật ký bảo trì — ADMIN, TECHNICIAN
        /// </summary>
        [HttpPost("{id}/logs")]
        public async Task<IActionResult> AddLog(
            Guid id, [FromBody] CreateMaintenanceLogRequest request)
        {
            var command = request.Adapt<CreateMaintenanceLogCommand>();
            command.TicketId = id;
            var result = await _maintenanceTicketUseCase.AddLog(command);
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/maintenance-tickets/{id}/logs
        /// Xem nhật ký bảo trì — ADMIN, TECHNICIAN, CUSTOMER
        /// </summary>
        [HttpGet("{id}/logs")]
        public async Task<IActionResult> GetLogs(Guid id)
        {
            var result = await _maintenanceTicketUseCase.GetLogs(id);
            return FromResult(new BaseResponse<List<MaintenanceLog>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        private (Guid? userId, string role) GetCurrentUser()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim   = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            Guid? userId    = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;
            return (userId, roleClaim);
        }
    }
}
