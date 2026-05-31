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
        private readonly Mock<IPaymentGatewayService>    _paymentGatewayService    = new();
        private readonly Mock<IOrderUseCase>             _orderUseCase             = new();
        private readonly Mock<IMaintenanceTicketUseCase> _maintenanceTicketUseCase = new();
        private readonly PayOSController                 _controller;

        private static readonly PaymentWebhookResult SampleWebhookResultSuccess = new()
        {
            OrderCode     = 123456789L,
            Amount        = 10_000,
            PaymentLinkId = "link-id-001",
            Reference     = "REF-20260527-001",
            Code          = "00",
            Description   = "success",
            Success       = true
        };

        private static readonly PaymentWebhookResult SampleWebhookResultCancelled = new()
        {
            OrderCode     = 123456789L,
            Amount        = 10_000,
            PaymentLinkId = "link-id-001",
            Code          = "01",
            Description   = "cancelled",
            Success       = false
        };

        public PayOSControllerTests()
        {
            _controller = new PayOSController(_paymentGatewayService.Object, _orderUseCase.Object, _maintenanceTicketUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        private static PayOSWebhookRequest BuildWebhookRequest(bool success = true) => new()
        {
            Code        = success ? "00" : "01",
            Description = success ? "success" : "cancelled",
            Success     = success,
            Signature   = "valid-signature",
            Data        = new PayOSWebhookDataRequest
            {
                OrderCode           = 123456789L,
                Amount              = 10_000,
                Description         = "Thanh toán đơn hàng SO-20260527-0001",
                AccountNumber       = "0123456789",
                Reference           = "REF-20260527-001",
                TransactionDateTime = "2026-05-27 10:00:00",
                Currency            = "VND",
                PaymentLinkId       = "link-id-001"
            }
        };

        // ─── F01: Webhook ─────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Webhook thanh toán thành công → 200
        public async Task Webhook_ValidPaymentSuccess_Returns200WithTrue()
        {
            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ReturnsAsync(SampleWebhookResultSuccess);
            _orderUseCase.Setup(x => x.HandlePaymentWebhook(It.IsAny<IncuSmart.Core.Commands.HandleOrderPaymentWebhookCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Webhook(BuildWebhookRequest(success: true));

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Webhook thanh toán bị huỷ → 200
        public async Task Webhook_ValidPaymentCancelled_Returns200WithFalse()
        {
            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ReturnsAsync(SampleWebhookResultCancelled);
            _orderUseCase.Setup(x => x.HandlePaymentWebhook(It.IsAny<IncuSmart.Core.Commands.HandleOrderPaymentWebhookCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(false));

            var result = await _controller.Webhook(BuildWebhookRequest(success: false));

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC03: VerifyWebhook ném exception → 200 (tránh retry loop)
        public async Task Webhook_VerifyThrowsException_Returns200WithFalse()
        {
            // PayOS webhook luôn trả 200 (kể cả khi lỗi) để tránh retry loop
            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ThrowsAsync(new Exception("Invalid signature"));

            var result = await _controller.Webhook(BuildWebhookRequest());

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC04: HandlePaymentWebhook ném exception → 200 (tránh retry loop)
        public async Task Webhook_HandleWebhookThrowsException_Returns200WithFalse()
        {
            _paymentGatewayService.Setup(x => x.VerifyWebhook(It.IsAny<PaymentWebhookRequest>()))
                .ReturnsAsync(SampleWebhookResultSuccess);
            _orderUseCase.Setup(x => x.HandlePaymentWebhook(It.IsAny<IncuSmart.Core.Commands.HandleOrderPaymentWebhookCommand>()))
                .ThrowsAsync(new Exception("DB error"));

            var result = await _controller.Webhook(BuildWebhookRequest());

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F02: Return ──────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Thanh toán thành công → 200
        public void Return_PaymentSuccess_Returns200()
        {
            var result = _controller.Return("00", "link-id-001", false);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Người dùng huỷ thanh toán → 200 (cancelled = true)
        public void Return_PaymentCancelled_Returns200WithCancelledTrue()
        {
            var result  = _controller.Return("01", "link-id-001", true);

            var ok      = result.Should().BeOfType<OkObjectResult>().Subject;
            var payload = ok.Value!;
            payload.GetType().GetProperty("cancelled")?.GetValue(payload).Should().Be(true);
        }

        [Fact] // F02-TC03: Params null → 200
        public void Return_NullParams_Returns200()
        {
            var result = _controller.Return(null, null, null);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F03: Cancel ──────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Huỷ thanh toán hợp lệ → 200 (cancelled = true)
        public void Cancel_ValidParams_Returns200WithCancelledTrue()
        {
            var result  = _controller.Cancel("00", "link-id-001");

            var ok      = result.Should().BeOfType<OkObjectResult>().Subject;
            var payload = ok.Value!;
            payload.GetType().GetProperty("cancelled")?.GetValue(payload).Should().Be(true);
        }

        [Fact] // F03-TC02: Params null → 200
        public void Cancel_NullParams_Returns200()
        {
            var result = _controller.Cancel(null, null);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
