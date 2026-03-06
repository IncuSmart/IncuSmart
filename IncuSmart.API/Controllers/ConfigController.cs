using IncuSmart.Core.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/configs")]
    public class ConfigController : ApiControllerBase
    {
        private readonly IConfigUseCase _configUseCase;
        public ConfigController(IConfigUseCase configUseCase) => _configUseCase = configUseCase;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var result = await _configUseCase.Create(request.Adapt<CreateConfigCommand>());
            return FromResult(new BaseResponse<Guid?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _configUseCase.GetById(id);
            return FromResult(new BaseResponse<Config?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _configUseCase.GetAll();
            return FromResult(new BaseResponse<List<Config>> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConfigRequest request)
        {
            var command = request.Adapt<UpdateConfigCommand>();
            command.Id = id;
            var result = await _configUseCase.Update(command);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _configUseCase.Delete(id);
            return FromResult(new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data });
        }
    }

}
