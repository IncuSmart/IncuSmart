using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHatchingSeasonRequest request)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _seasonUseCase.Create(request.Adapt<CreateHatchingSeasonCommand>(), userId, role);
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-seasons
        /// Xem danh sách mùa ấp — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ thấy mùa ấp của máy mình
        /// Lọc: incubatorId?
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _seasonUseCase.GetById(id, userId, role);
            return FromResult(new BaseResponse<HatchingSeasonDetailResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid?   incubatorId,
            [FromQuery] Guid?   customerId,
            [FromQuery] string? status,
            [FromQuery] PagingRequest paging)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _seasonUseCase.List(incubatorId, customerId, status, userId, role, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<HatchingSeason>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/hatching-seasons/{id}
        /// Cập nhật mùa ấp — ADMIN, TECHNICIAN, CUSTOMER (chủ máy)
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHatchingSeasonRequest request)
        {
            var command = request.Adapt<UpdateHatchingSeasonCommand>();
            command.Id  = id;
            var (userId, role) = GetCurrentUser();
            var result  = await _seasonUseCase.Update(command, userId, role);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateHatchingSeasonStatusRequest request)
        {
            var command = request.Adapt<UpdateHatchingSeasonStatusCommand>();
            command.Id = id;
            var (userId, role) = GetCurrentUser();
            var result = await _seasonUseCase.UpdateStatus(command, userId, role);
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
