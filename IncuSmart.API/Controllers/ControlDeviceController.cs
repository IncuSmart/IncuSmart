using IncuSmart.API.Requests;
using IncuSmart.API.Responses;
using IncuSmart.Core.Commands;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace IncuSmart.API.Controllers;

[ApiController]
[Route("api/control-devices")]
public class ControlDeviceController : ApiControllerBase
{
    private readonly IControlDeviceUseCase _controlDeviceUseCase;

    public ControlDeviceController(IControlDeviceUseCase controlDeviceUseCase)
    {
        _controlDeviceUseCase = controlDeviceUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateControlDeviceRequest request)
    {
        var command = request.Adapt<CreateControlDeviceCommand>();
        var result = await _controlDeviceUseCase.Create(command);
        return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
    }

    [HttpGet("/api/incubators/{incubatorId:guid}/control-devices")]
    public async Task<IActionResult> GetByIncubator(Guid incubatorId)
    {
        var result = await _controlDeviceUseCase.GetByIncubatorId(incubatorId);
        return FromResult(new BaseResponse<IEnumerable<ControlDevice>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
    }
}
