using IncuSmart.Core.Domains;
using System;
using System.Security.Claims;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    public class WarrantyController(IWarrantyUseCase _warrantyUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/warranties
        /// Tạo bảo hành cho máy — ADMIN, SALES_STAFF
        /// </summary>
        [HttpPost("api/warranties")]
        public async Task<IActionResult> Create([FromBody] CreateWarrantyRequest request)
        {
            var result = await _warrantyUseCase.Create(request.Adapt<CreateWarrantyCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/incubators/{id}/warranty
        /// Xem bảo hành của máy — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: chỉ xem được máy của mình
        /// </summary>
        [HttpGet("api/incubators/{incubatorId}/warranty")]
        public async Task<IActionResult> GetByIncubatorId(Guid incubatorId)
        {
            var (userId, role) = GetCurrentUser();
            var result = await _warrantyUseCase.GetByIncubatorId(incubatorId, userId, role);
            return FromResult(new BaseResponse<Warranty?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
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
