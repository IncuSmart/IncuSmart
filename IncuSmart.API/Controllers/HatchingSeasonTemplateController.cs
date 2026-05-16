using IncuSmart.Core.Domains;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/hatching-season-templates")]
    public class HatchingSeasonTemplateController(
        IHatchingSeasonTemplateUseCase _templateUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/hatching-season-templates
        /// Tạo mẫu mùa ấp — ADMIN, TECHNICIAN, CUSTOMER
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHatchingSeasonTemplateRequest request)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _templateUseCase.Create(request.Adapt<CreateHatchingSeasonTemplateCommand>(), userId, role);
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-season-templates/{id}
        /// Xem chi tiết mẫu mùa ấp kèm batches + configs
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _templateUseCase.GetById(id, userId, role);
            return FromResult(new BaseResponse<HatchingSeasonTemplateDetailResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-season-templates
        /// Xem danh sách mẫu — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: thấy template cá nhân + template public (TECHNICIAN)
        /// Lọc: customerId?, createdByType? (CUSTOMER | TECHNICIAN)
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid?   customerId,
            [FromQuery] string? createdByType,
            [FromQuery] PagingRequest paging)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _templateUseCase.List(customerId, createdByType, userId, role, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<HatchingSeasonTemplate>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/hatching-season-templates/{id}
        /// Cập nhật mẫu mùa ấp — ADMIN, TECHNICIAN, CUSTOMER (chủ sở hữu)
        /// Nếu gửi Batches → soft-delete cũ, insert mới
        /// </summary>
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHatchingSeasonTemplateRequest request)
        {
            var command = request.Adapt<UpdateHatchingSeasonTemplateCommand>();
            command.Id  = id;
            var (userId, role) = GetCurrentUser();
            var result  = await _templateUseCase.Update(command, userId, role);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _templateUseCase.Delete(id, userId, role);
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
