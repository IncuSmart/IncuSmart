using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    public class ControlDeviceController(IControlDeviceUseCase _controlDeviceUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/control-devices
        /// Tạo thiết bị điều khiển mới — ADMIN, TECHNICIAN
        /// Validate: masterboard tồn tại, config tồn tại
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("api/control-devices")]
        public async Task<IActionResult> Create([FromBody] CreateControlDeviceRequest request)
        {
            var result = await _controlDeviceUseCase.Create(request.Adapt<CreateControlDeviceCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/incubators/{id}/control-devices
        /// Xem danh sách thiết bị điều khiển của một máy — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ xem được máy của mình
        /// Response: include Config info, ControlBoardType info
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("api/incubators/{incubatorId}/control-devices")]
        public async Task<IActionResult> GetByIncubatorId(Guid incubatorId)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _controlDeviceUseCase.GetByIncubatorId(incubatorId, userId, role);
            return FromResult(new BaseResponse<List<ControlDevice>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        // ─── Helper ──────────────────────────────────────────────────────────────────
        private (Guid? userId, string role) GetCurrentUser()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim   = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            Guid? userId    = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;
            return (userId, roleClaim);
        }
    }
}
