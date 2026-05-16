using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    public class HatchingBatchController(
        IHatchingBatchUseCase _batchUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/hatching-batches
        /// Tạo giai đoạn ấp — ADMIN, TECHNICIAN, CUSTOMER
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("api/hatching-batches")]
        public async Task<IActionResult> Create([FromBody] CreateHatchingBatchRequest request)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _batchUseCase.Create(request.Adapt<CreateHatchingBatchCommand>(), userId, role);
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-seasons/{seasonId}/batches
        /// Xem danh sách giai đoạn ấp kèm configs — ADMIN, TECHNICIAN, CUSTOMER
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("api/hatching-seasons/{seasonId}/batches")]
        public async Task<IActionResult> GetBySeasonId(Guid seasonId)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _batchUseCase.GetBySeasonId(seasonId, userId, role);
            return FromResult(new BaseResponse<List<HatchingBatchDetailResponse>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/hatching-batches/{id}
        /// Cập nhật giai đoạn ấp — ADMIN, TECHNICIAN, CUSTOMER
        /// Nếu gửi Configs → soft-delete cũ, insert mới
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPut("api/hatching-batches/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHatchingBatchRequest request)
        {
            var command = request.Adapt<UpdateHatchingBatchCommand>();
            command.Id  = id;
            var (userId, role) = GetCurrentUser();
            var result  = await _batchUseCase.Update(command, userId, role);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpDelete("api/hatching-batches/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _batchUseCase.Delete(id, userId, role);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        private (Guid? userId, string role) GetCurrentUser()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;
            return (userId, roleClaim);
        }
    }
}
