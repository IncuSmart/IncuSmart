using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/hatching-seasons")]
    public class HatchingSeasonController(
        IHatchingSeasonUseCase _seasonUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/hatching-seasons
        /// Tạo mùa ấp mới — ADMIN, TECHNICIAN, CUSTOMER
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHatchingSeasonRequest request)
        {
            var result = await _seasonUseCase.Create(request.Adapt<CreateHatchingSeasonCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-seasons
        /// Xem danh sách mùa ấp — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ thấy mùa ấp của máy mình
        /// Lọc: incubatorId?
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? incubatorId)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _seasonUseCase.GetAll(incubatorId, userId, role);
            return FromResult(new BaseResponse<List<HatchingSeason>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/hatching-seasons/{id}
        /// Cập nhật mùa ấp — ADMIN, TECHNICIAN, CUSTOMER (chủ máy)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHatchingSeasonRequest request)
        {
            var command = request.Adapt<UpdateHatchingSeasonCommand>();
            command.Id  = id;
            var result  = await _seasonUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
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
