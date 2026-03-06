using IncuSmart.Core;
using IncuSmart.Core.Utils;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ApiControllerBase
    {
        private readonly IOrderUseCase _orderUseCase;

        public OrderController(IOrderUseCase orderUseCase)
        {
            _orderUseCase = orderUseCase;
        }

        [Authorize(Roles = "CUSTOMER")]
        [HttpPost("customer")]
        public async Task<IActionResult> CreateOrderByCustomer(
            [FromBody] CreateOrderByCustomerRequest request)
        {
            request.UserId = HttpContext.GetId();

            ResultModel<Guid?> result = await _orderUseCase.CreateOrderByCustomer(
                request.Adapt<CreateOrderByCustomerCommand>()
            );

            return FromResult(new BaseResponse<Guid?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpPost("guest")]
        public async Task<IActionResult> CreateOrderByGuest(
            [FromBody] CreateOrderByGuestRequest request)
        {
            ResultModel<Guid?> result = await _orderUseCase.CreateOrderByGuest(
                request.Adapt<CreateOrderByGuestCommand>()
            );

            BaseResponse<OrderResponse> response = new();

            return FromResult(new BaseResponse<Guid?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }
    }
}
