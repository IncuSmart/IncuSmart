using IncuSmart.API.Responses;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ApiControllerBase
    {
        private readonly IAlertUseCase _alertUseCase;

        public AlertsController(IAlertUseCase alertUseCase)
        {
            _alertUseCase = alertUseCase;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _alertUseCase.GetAlertById(id, User);

            var response = new BaseResponse<AlertResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data?.Adapt<AlertResponse>()
            };

            return FromResult(response);
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,TECHNICIAN,CUSTOMER")]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? incubatorId,
            [FromQuery] string? severity,
            [FromQuery] string? status,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _alertUseCase.GetAllAlerts(User, incubatorId, severity, status, from, to);

            var response = new BaseResponse<IEnumerable<AlertResponse>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data?.Adapt<IEnumerable<AlertResponse>>()
            };

            return FromResult(response);
        }

        [HttpPut("{id}/resolve")]
        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        public async Task<IActionResult> ResolveAlert(Guid id, [FromBody] ResolveAlertRequest request)
        {
            var result = await _alertUseCase.ResolveAlert(id, request.Message, User);

            var response = new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            };

            return FromResult(response);
        }
    }
}
