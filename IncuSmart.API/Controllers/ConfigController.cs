using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/configs")]
    public class ConfigController(IConfigUseCase _configUseCase) : ApiControllerBase
    {
        /// <summary>
        /// POST /api/configs
        /// Tạo mới cấu hình thiết bị — ADMIN only
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var result = await _configUseCase.Create(request.Adapt<CreateConfigCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/configs/{id}
        /// Xem chi tiết cấu hình thiết bị — ADMIN, SALES_STAFF, TECHNICIAN
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _configUseCase.GetById(id);
            return FromResult(new BaseResponse<Config?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// GET /api/configs
        /// Xem danh sách cấu hình thiết bị — ADMIN, SALES_STAFF, TECHNICIAN
        /// Lọc theo: type (SENSOR | ACTUATOR), status (ACTIVE | INACTIVE)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? type,
            [FromQuery] string? status)
        {
            var result = await _configUseCase.GetAll(type, status);
            return FromResult(new BaseResponse<List<Config>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// PUT /api/configs/{id}
        /// Cập nhật cấu hình thiết bị — ADMIN only
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConfigRequest request)
        {
            var command = request.Adapt<UpdateConfigCommand>();
            command.Id  = id;
            var result  = await _configUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        /// <summary>
        /// DELETE /api/configs/{id}
        /// Xoá mềm cấu hình thiết bị — ADMIN only
        /// Không xoá được nếu đang được tham chiếu trong incubator_model_configs
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _configUseCase.Delete(id);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
