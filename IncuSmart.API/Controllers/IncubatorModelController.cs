using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/incubator-models")]
    public class IncubatorModelController : ApiControllerBase
    {
        private readonly IIncubatorModelUseCase _modelUseCase;
        public IncubatorModelController(IIncubatorModelUseCase modelUseCase) => _modelUseCase = modelUseCase;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateIncubatorModelRequest request)
        {
            var result = await _modelUseCase.Create(request.Adapt<CreateIncubatorModelCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _modelUseCase.GetById(id);
            return FromResult(new BaseResponse<IncubatorModel?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _modelUseCase.GetAll();
            return FromResult(new BaseResponse<List<IncubatorModel>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncubatorModelRequest request)
        {
            var command = request.Adapt<UpdateIncubatorModelCommand>();
            command.Id = id;
            var result = await _modelUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _modelUseCase.Delete(id);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }

}
