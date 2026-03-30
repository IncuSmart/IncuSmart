using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;

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
        [HttpPost("api/hatching-batches")]
        public async Task<IActionResult> Create([FromBody] CreateHatchingBatchRequest request)
        {
            var result = await _batchUseCase.Create(request.Adapt<CreateHatchingBatchCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/hatching-seasons/{seasonId}/batches
        /// Xem danh sách giai đoạn ấp kèm configs — ADMIN, TECHNICIAN, CUSTOMER
        /// </summary>
        [HttpGet("api/hatching-seasons/{seasonId}/batches")]
        public async Task<IActionResult> GetBySeasonId(Guid seasonId)
        {
            var result = await _batchUseCase.GetBySeasonId(seasonId);
            return FromResult(new BaseResponse<List<HatchingBatchDetailResponse>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data.Adapt<List<HatchingBatchDetailResponse>>() });
        }

        /// <summary>
        /// PUT /api/hatching-batches/{id}
        /// Cập nhật giai đoạn ấp — ADMIN, TECHNICIAN, CUSTOMER
        /// Nếu gửi Configs → soft-delete cũ, insert mới
        /// </summary>
        [HttpPut("api/hatching-batches/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHatchingBatchRequest request)
        {
            var command = request.Adapt<UpdateHatchingBatchCommand>();
            command.Id  = id;
            var result  = await _batchUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
