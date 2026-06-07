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
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("fan-speed")]
        public async Task<IActionResult> SetFanSpeed(Guid incubatorId, [FromBody] SetFanSpeedRequest request)
        {
            var result = await _commandUseCase.SetFanSpeed(incubatorId, request.Speed);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("temperature")]
        public async Task<IActionResult> SetTemperature(Guid incubatorId, [FromBody] SetTemperatureRequest request)
        {
            var result = await _commandUseCase.SetTemperature(incubatorId, request.Value);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("turn-tray")]
        public async Task<IActionResult> TurnTray(Guid incubatorId)
        {
            var result = await _commandUseCase.TurnTray(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("auto-turn/stop")]
        public async Task<IActionResult> StopAutoTurn(Guid incubatorId)
        {
            var result = await _commandUseCase.StopAutoTurn(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("auto-turn/start")]
        public async Task<IActionResult> StartAutoTurn(Guid incubatorId)
        {
            var result = await _commandUseCase.StartAutoTurn(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("incubation/start")]
        public async Task<IActionResult> StartIncubation(Guid incubatorId)
        {
            var result = await _commandUseCase.StartIncubation(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        [HttpPost("incubation/reset")]
        public async Task<IActionResult> ResetIncubation(Guid incubatorId)
        {
            var result = await _commandUseCase.ResetIncubation(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("fan-check/disable")]
        public async Task<IActionResult> DisableFanCheck(Guid incubatorId)
        {
            var result = await _commandUseCase.DisableFanCheck(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("fan-check/enable")]
        public async Task<IActionResult> EnableFanCheck(Guid incubatorId)
        {
            var result = await _commandUseCase.EnableFanCheck(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("reset-wifi")]
        public async Task<IActionResult> ResetWifi(Guid incubatorId)
        {
            var result = await _commandUseCase.ResetWifi(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("reboot")]
        public async Task<IActionResult> Reboot(Guid incubatorId)
        {
            var result = await _commandUseCase.Reboot(incubatorId);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}
