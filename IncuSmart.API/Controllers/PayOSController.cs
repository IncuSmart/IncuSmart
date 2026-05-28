using IncuSmart.Core.Ports.Outbound;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/payos")]
    public class PayOSController(
        IPaymentGatewayService _paymentGatewayService,
        IOrderUseCase _orderUseCase,
        IMaintenanceTicketUseCase _maintenanceTicketUseCase) : ApiControllerBase
    {
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] PayOSWebhookRequest request)
        {
            try
            {
                var verified = await _paymentGatewayService.VerifyWebhook(new PaymentWebhookRequest
                {
                    Code = request.Code,
                    Description = request.Description,
                    Success = request.Success,
                    Signature = request.Signature,
                    Data = new IncuSmart.Core.Ports.Outbound.PaymentWebhookDataRequest
                    {
                        OrderCode = request.Data.OrderCode,
                        Amount = request.Data.Amount,
                        Description = request.Data.Description,
                        AccountNumber = request.Data.AccountNumber,
                        Reference = request.Data.Reference,
                        TransactionDateTime = request.Data.TransactionDateTime,
                        Currency = request.Data.Currency,
                        PaymentLinkId = request.Data.PaymentLinkId,
                        Code = request.Data.Code,
                        Description2 = request.Data.Description2,
                        CounterAccountBankId = request.Data.CounterAccountBankId,
                        CounterAccountBankName = request.Data.CounterAccountBankName,
                        CounterAccountName = request.Data.CounterAccountName,
                        CounterAccountNumber = request.Data.CounterAccountNumber,
                        VirtualAccountName = request.Data.VirtualAccountName,
                        VirtualAccountNumber = request.Data.VirtualAccountNumber
                    }
                });

                var webhookCommand = new HandleOrderPaymentWebhookCommand
                {
                    PaymentOrderCode = verified.OrderCode,
                    Amount = verified.Amount,
                    PaymentLinkId = verified.PaymentLinkId,
                    Reference = verified.Reference,
                    TransactionDateTime = verified.TransactionDateTime,
                    ProviderCode = verified.Code,
                    ProviderDescription = verified.Description,
                    Success = verified.Success
                };

                var orderResult = await _orderUseCase.HandlePaymentWebhook(webhookCommand);
                if (orderResult.StatusCode == "404")
                {
                    await _maintenanceTicketUseCase.HandlePaymentWebhook(webhookCommand);
                }

                return Ok(new BaseResponse<bool> { StatusCode = "200", Message = CommonConst.Success, Data = true });
            }
            catch (Exception)
            {
                return Ok(new BaseResponse<bool> { StatusCode = "200", Message = CommonConst.Success, Data = false });
            }
        }

        [HttpGet("return")]
        public IActionResult Return([FromQuery] string? code, [FromQuery] string? id, [FromQuery] bool? cancel)
        {
            return Ok(new
            {
                message = CommonConst.Success,
                code,
                paymentLinkId = id,
                cancelled = cancel ?? false
            });
        }

        [HttpGet("cancel")]
        public IActionResult Cancel([FromQuery] string? code, [FromQuery] string? id)
        {
            return Ok(new
            {
                message = CommonConst.Success,
                code,
                paymentLinkId = id,
                cancelled = true
            });
        }
    }
}
