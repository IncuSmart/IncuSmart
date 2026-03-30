using IncuSmart.Core.Domains;
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHatchingSeasonTemplateRequest request)
        {
            var result = await _templateUseCase.Create(request.Adapt<CreateHatchingSeasonTemplateCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-season-templates/{id}
        /// Xem chi tiết mẫu mùa ấp kèm batches + configs
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _templateUseCase.GetById(id);
            return FromResult(new BaseResponse<HatchingSeasonTemplateDetailResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data.Adapt<HatchingSeasonTemplateDetailResponse>() });
        }

        /// <summary>
        /// GET /api/hatching-season-templates
        /// Xem danh sách mẫu — ADMIN, TECHNICIAN, CUSTOMER
        /// CUSTOMER: thấy template cá nhân + template public (TECHNICIAN)
        /// Lọc: customerId?, createdByType? (CUSTOMER | TECHNICIAN)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid?   customerId,
            [FromQuery] string? createdByType)
        {
            var result = await _templateUseCase.GetAll(customerId, createdByType);
            return FromResult(new BaseResponse<List<HatchingSeasonTemplate>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/hatching-season-templates/{id}
        /// Cập nhật mẫu mùa ấp — ADMIN, TECHNICIAN, CUSTOMER (chủ sở hữu)
        /// Nếu gửi Batches → soft-delete cũ, insert mới
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHatchingSeasonTemplateRequest request)
        {
            var command = request.Adapt<UpdateHatchingSeasonTemplateCommand>();
            command.Id  = id;
            var result  = await _templateUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
