using IncuSmart.API.Responses;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using Mapster;
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
    }
}
