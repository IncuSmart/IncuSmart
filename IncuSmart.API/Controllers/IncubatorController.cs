using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/incubator")]
    public class IncubatorController(IIncubatorUseCase _incubatorUseCase) : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateIncubatorRequest request)
        {
            var result = await _incubatorUseCase.Create(request.Adapt<CreateIncubatorCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _incubatorUseCase.GetById(id);
            return FromResult(new BaseResponse<Incubator?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _incubatorUseCase.GetAll();
            return FromResult(new BaseResponse<List<Incubator>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncubatorRequest request)
        {
            var command = request.Adapt<UpdateIncubatorCommand>();
            command.Id = id;
            var result = await _incubatorUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _incubatorUseCase.Delete(id);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }
}

