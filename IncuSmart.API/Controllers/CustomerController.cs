using Microsoft.AspNetCore.Authorization;
using IncuSmart.Core.Ports.Outbound;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomerController(ICustomerUseCase _customerUseCase, ICustomerRepository _customerRepository) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,SALES_STAFF")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? search, [FromQuery] PagingRequest paging)
        {
            var result = await _customerUseCase.List(status, search, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<CustomerSummaryResponse>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _customerUseCase.GetById(id);
            return FromResult(new BaseResponse<CustomerDetailResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "CUSTOMER")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var result = await _customerUseCase.GetByUserId(HttpContext.GetId());
            return FromResult(new BaseResponse<CustomerDetailResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF")]
        [HttpGet("{id}/orders")]
        public async Task<IActionResult> GetOrders(Guid id)
        {
            var result = await _customerUseCase.GetOrders(id);
            return FromResult(new BaseResponse<List<SalesOrder>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerProfileRequest request)
        {
            var result = await _customerUseCase.UpdateProfile(new UpdateCustomerProfileCommand
            {
                CustomerId = id,
                Address = request.Address
            });

            return FromResult(new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "CUSTOMER")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateCustomerProfileRequest request)
        {
            var customer = await _customerRepository.FindByUserId(HttpContext.GetId());
            if (customer == null)
            {
                return FromResult(new BaseResponse<bool>
                {
                    StatusCode = "404",
                    Message = CommonConst.CustomerNotFound,
                    Data = false
                });
            }

            var result = await _customerUseCase.UpdateProfile(new UpdateCustomerProfileCommand
            {
                CustomerId = customer.Id,
                Address = request.Address
            });

            return FromResult(new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }
    }
}
