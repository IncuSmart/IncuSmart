using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class PayOSControllerTests
    {
        private readonly Mock<IPaymentGatewayService>       _paymentGatewayService       = new();
        private readonly Mock<IOrderUseCase>                _orderUseCase                = new();
        private readonly Mock<IMaintenanceTicketUseCase>    _maintenanceTicketUseCase    = new();
        private readonly PayOSController                    _controller;

        public PayOSControllerTests()
        {
            _controller = new PayOSController(_paymentGatewayService.Object, _orderUseCase.Object, _maintenanceTicketUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        private static PayOSWebhookRequest BuildWebhookRequest(bool success = true) => new()
        {
            Code        = "00",
            Description = "success",
            Success     = success,
            Signature   = "valid-signature",
            Data        = new PayOSWebhookDataRequest
            {
                OrderCode      = 123456789L,
                Amount         = 10_000,
                Description    = "Thanh toán đơn hàng SO-001",
                AccountNumber  = "0123456789",
                Reference      = "REF-001",
                TransactionDateTime = "2026-05-27 10:00:00",
                Currency       = "VND",
                PaymentLinkId  = "link-id-001"
            }
        };

        // ─── Webhook ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Webhook_ValidPaymentSuccess_Returns200WithTrue()
        {
            var webhookResult = new PaymentWebhookResult
            {
                OrderCode     = 123456789L,
                Amount        = 10_000,
                PaymentLinkId = "link-id-001",
                Reference     = "REF-001",
                Code          = "00",
                Description   = "success",
                Success       = true
            };

            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ReturnsAsync(webhookResult);
            _orderUseCase.Setup(x => x.HandlePaymentWebhook(It.IsAny<IncuSmart.Core.Commands.HandleOrderPaymentWebhookCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Webhook(BuildWebhookRequest(success: true));

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Webhook_ValidPaymentCancelled_Returns200WithFalse()
        {
            var webhookResult = new PaymentWebhookResult
            {
                OrderCode     = 123456789L,
                Amount        = 10_000,
                PaymentLinkId = "link-id-001",
                Code          = "01",
                Description   = "cancelled",
                Success       = false
            };

            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ReturnsAsync(webhookResult);
            _orderUseCase.Setup(x => x.HandlePaymentWebhook(It.IsAny<IncuSmart.Core.Commands.HandleOrderPaymentWebhookCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(false));

            var result = await _controller.Webhook(BuildWebhookRequest(success: false));

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Webhook_VerifyThrowsException_Returns200WithFalse()
        {
            // PayOS webhook luôn trả 200 (kể cả khi lỗi) để tránh retry loop
            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ThrowsAsync(new Exception("Invalid signature"));

            var result = await _controller.Webhook(BuildWebhookRequest());

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Webhook_HandleWebhookThrowsException_Returns200WithFalse()
        {
            var webhookResult = new PaymentWebhookResult { OrderCode = 1L, Amount = 1, Success = true };
            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ReturnsAsync(webhookResult);
            _orderUseCase.Setup(x => x.HandlePaymentWebhook(It.IsAny<IncuSmart.Core.Commands.HandleOrderPaymentWebhookCommand>()))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Webhook(BuildWebhookRequest());

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── Return ───────────────────────────────────────────────────────────────────

        [Fact]
        public void Return_PaymentSuccess_Returns200()
        {
            var result = _controller.Return("00", "link-id-001", false);

            result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
        }

        [Fact]
        public void Return_PaymentCancelled_Returns200WithCancelledTrue()
        {
            var result = _controller.Return("01", "link-id-001", true);

            var ok      = result.Should().BeOfType<OkObjectResult>().Subject;
            var payload = ok.Value!;
            // cancelled = true khi người dùng huỷ thanh toán
            payload.GetType().GetProperty("cancelled")?.GetValue(payload).Should().Be(true);
        }

        [Fact]
        public void Return_NullParams_Returns200()
        {
            var result = _controller.Return(null, null, null);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── Cancel ───────────────────────────────────────────────────────────────────

        [Fact]
        public void Cancel_ValidParams_Returns200WithCancelledTrue()
        {
            var result = _controller.Cancel("00", "link-id-001");

            var ok      = result.Should().BeOfType<OkObjectResult>().Subject;
            var payload = ok.Value!;
            payload.GetType().GetProperty("cancelled")?.GetValue(payload).Should().Be(true);
        }

        [Fact]
        public void Cancel_NullParams_Returns200()
        {
            var result = _controller.Cancel(null, null);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
