using IncuSmart.Core;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController(IOrderUseCase _orderUseCase, IAuditLogUseCase _auditLogUseCase) : ApiControllerBase
    {
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost("customer")]
        public async Task<IActionResult> CreateOrderByCustomer([FromBody] CreateOrderByCustomerRequest request)
        {
            request.UserId = HttpContext.GetId();
            var result = await _orderUseCase.CreateOrderByCustomer(request.Adapt<CreateOrderByCustomerCommand>());
            return await FromResultAndAudit(
                new BaseResponse<CreateOrderResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.SALES_ORDER,
                result.Data?.OrderId);
        }

        [HttpPost("guest")]
        public async Task<IActionResult> CreateOrderByGuest([FromBody] CreateOrderByGuestRequest request)
        {
            var result = await _orderUseCase.CreateOrderByGuest(request.Adapt<CreateOrderByGuestCommand>());
            return FromResult(new BaseResponse<CreateOrderResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "SALES_STAFF,ADMIN")]
        [HttpPost("sales")]
        public async Task<IActionResult> CreateOrderBySales([FromBody] CreateOrderBySalesRequest request)
        {
            request.CreatedByUserId = HttpContext.GetId();
            var result = await _orderUseCase.CreateOrderBySales(request.Adapt<CreateOrderBySalesCommand>());
            return await FromResultAndAudit(
                new BaseResponse<CreateOrderResponse?> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CREATE,
                AuditEntityType.SALES_ORDER,
                result.Data?.OrderId);
        }

        [Authorize(Roles = "SALES_STAFF,ADMIN")]
        [HttpPost("{orderId:guid}/items/assign")]
        public async Task<IActionResult> AssignIncubatorToOrderItem(Guid orderId, [FromBody] AssignIncubatorToOrderItemRequest request)
        {
            var command = request.Adapt<AssignIncubatorToOrderItemCommand>();
            command.OrderId = orderId;
            var result = await _orderUseCase.AssignIncubatorToOrderItem(command);
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.ASSIGN_INCUBATOR,
                AuditEntityType.SALES_ORDER,
                orderId);
        }

        [Authorize(Roles = "SALES_STAFF,ADMIN")]
        [HttpPost("{orderId:guid}/complete")]
        public async Task<IActionResult> CompleteOrder(Guid orderId)
        {
            var result = await _orderUseCase.CompleteOrder(new CompleteOrderCommand { OrderId = orderId });
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.COMPLETE,
                AuditEntityType.SALES_ORDER,
                orderId);
        }

        [Authorize(Roles = "CUSTOMER")]
        [HttpPost("guest/claim")]
        public async Task<IActionResult> ClaimGuestOrder([FromBody] ClaimGuestOrderRequest request)
        {
            request.UserId = HttpContext.GetId();
            var result = await _orderUseCase.ClaimGuestOrder(request.Adapt<ClaimGuestOrderCommand>());
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CLAIM_GUEST_ORDER,
                AuditEntityType.SALES_ORDER,
                request.OrderId);
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,CUSTOMER")]
        [HttpPost("{orderId:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            var userId = HttpContext.GetId();
            var role = HttpContext.GetRole();
            var result = await _orderUseCase.CancelOrder(new CancelOrderCommand
            {
                OrderId = orderId,
                CancelledByUserId = userId == Guid.Empty ? null : userId,
                Role = role
            });
            return await FromResultAndAudit(
                new BaseResponse<bool> { StatusCode = result.StatusCode, Message = result.Message, Data = result.Data },
                _auditLogUseCase,
                HttpContext.GetId(),
                AuditAction.CANCEL,
                AuditEntityType.SALES_ORDER,
                orderId);
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,CUSTOMER")]
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? status,
            [FromQuery] Guid? customerId,
            [FromQuery] PagingRequest paging)
        {
            var userId = HttpContext.GetId();
            var role = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            var result = await _orderUseCase.List(status, customerId, userId, role, paging.Page, paging.PageSize);
            return FromResult(new BaseResponse<PagedResult<SalesOrder>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,CUSTOMER")]
        [HttpGet("{id:guid}/payment-status")]
        public async Task<IActionResult> GetPaymentStatus(Guid id)
        {
            var userId = HttpContext.GetId();
            var role = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            var result = await _orderUseCase.GetPaymentStatus(id, userId, role);
            return FromResult(new BaseResponse<OrderPaymentStatusResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [Authorize(Roles = "ADMIN,SALES_STAFF,CUSTOMER")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = HttpContext.GetId();
            var role = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            var result = await _orderUseCase.GetById(id, userId, role);
            return FromResult(new BaseResponse<SalesOrderDetailResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }
    }
}
