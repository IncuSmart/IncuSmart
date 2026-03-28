using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    public class SensorController(
        ISensorUseCase        _sensorUseCase,
        ISensorReadingUseCase _sensorReadingUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/sensors
        /// Tạo sensor vật lý — ADMIN, TECHNICIAN
        /// Validate: masterboard tồn tại, config instance tồn tại và thuộc cùng incubator
        /// </summary>
        [HttpPost("api/sensors")]
        public async Task<IActionResult> Create([FromBody] CreateSensorRequest request)
        {
            var result = await _sensorUseCase.Create(request.Adapt<CreateSensorCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/incubators/{id}/sensors
        /// Xem danh sách sensor của máy — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ xem được máy của mình
        /// Response: include Config info (name, unit, type)
        /// </summary>
        [HttpGet("api/incubators/{incubatorId}/sensors")]
        public async Task<IActionResult> GetByIncubatorId(Guid incubatorId)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _sensorUseCase.GetByIncubatorId(incubatorId, userId, role);
            return FromResult(new BaseResponse<List<Sensor>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/incubators/{id}/sensor-readings
        /// Xem dữ liệu vận hành cảm biến — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ xem được máy của mình
        /// Lọc: sensorId?, configId?, from?, to?, limit (mặc định 100, tối đa 1000)
        /// Sắp xếp: recorded_at DESC
        /// Response: include Sensor info + Config info (name, unit)
        /// </summary>
        [HttpGet("api/incubators/{incubatorId}/sensor-readings")]
        public async Task<IActionResult> GetSensorReadings(
            Guid incubatorId,
            [FromQuery] Guid?     sensorId,
            [FromQuery] Guid?     configId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int       limit = 100)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _sensorReadingUseCase.GetByFilters(
                incubatorId, sensorId, configId, from, to, limit, userId, role);
            return FromResult(new BaseResponse<List<SensorReading>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
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
