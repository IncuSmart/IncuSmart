using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/incubators/{incubatorId}/device")]
    public class DeviceController(IDeviceCommandUseCase _commandUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("power")]
        public async Task<IActionResult> SetPower(Guid incubatorId, [FromBody] SetPowerRequest request)
        {
            var result = await _commandUseCase.SetPower(incubatorId, request.On);
            return FromResult(new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message    = result.Message,
                Data       = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("heater-mode")]
        public async Task<IActionResult> SetHeaterMode(Guid incubatorId, [FromBody] SetHeaterModeRequest request)
        {
            var result = await _commandUseCase.SetHeaterMode(incubatorId, request.Mode);
            return FromResult(new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message    = result.Message,
                Data       = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("fan-mode")]
        public async Task<IActionResult> SetFanMode(Guid incubatorId, [FromBody] SetFanModeRequest request)
        {
            var result = await _commandUseCase.SetFanMode(incubatorId, request.Mode);
            return FromResult(new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message    = result.Message,
                Data       = result.Data
            });
        }
    }
}
