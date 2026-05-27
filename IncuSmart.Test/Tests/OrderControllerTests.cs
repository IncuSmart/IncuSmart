using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class OrderControllerTests
    {
        private readonly Mock<IOrderUseCase>    _orderUseCase    = new();
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly OrderController        _controller;

        private static readonly Guid OrderId = Guid.NewGuid();

        private static readonly CreateOrderResponse SampleOrderResponse = new()
        {
            OrderId              = OrderId,
            OrderCode            = "SO-20260527-0001",
            TotalAmount          = 10_000,
            PaymentStatus        = IncuSmart.Core.Enums.PaymentStatus.PENDING,
            PaymentOrderCode     = 123456789L,
            PaymentLinkId        = "link-id",
            QrCode               = "data:image/png;base64,abc",
            PaymentLinkExpiredAt = DateTime.UtcNow.AddMinutes(15)
        };

        public OrderControllerTests()
        {
            _controller = new OrderController(_orderUseCase.Object, _auditLogUseCase.Object);

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── CreateOrderByCustomer ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrderByCustomer_ValidItems_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<CreateOrderResponse?>(SampleOrderResponse));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest
            {
                Items = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateOrderByCustomer_ModelNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<CreateOrderResponse?>("Sản phẩm không tồn tại"));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest
            {
                Items = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── CreateOrderByGuest ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrderByGuest_ValidRequest_Returns200()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<CreateOrderResponse?>(SampleOrderResponse));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Nguyễn Văn A",
                Phone            = "0901234567",
                Email            = "a@test.com",
                VerificationPass = "pass1234",
                Items            = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateOrderByGuest_ModelNotFound_Returns404()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<CreateOrderResponse?>("Sản phẩm không tồn tại"));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Khách",
                Phone            = "0901234567",
                VerificationPass = "pass1234",
                Items            = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── CreateOrderBySales ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrderBySales_ValidRequest_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CreateOrderBySales(It.IsAny<IncuSmart.Core.Commands.CreateOrderBySalesCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<CreateOrderResponse?>(SampleOrderResponse));

            var result = await _controller.CreateOrderBySales(new CreateOrderBySalesRequest
            {
                CustomerId = ControllerTestBase.CustomerId,
                Items      = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CreateOrderBySales_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CreateOrderBySales(It.IsAny<IncuSmart.Core.Commands.CreateOrderBySalesCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<CreateOrderResponse?>("Khách hàng không tồn tại"));

            var result = await _controller.CreateOrderBySales(new CreateOrderBySalesRequest
            {
                CustomerId = Guid.NewGuid(),
                Items      = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── AssignIncubatorToOrderItem ───────────────────────────────────────────────

        [Fact]
        public async Task AssignIncubatorToOrderItem_ValidRequest_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task AssignIncubatorToOrderItem_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.AssignIncubatorToOrderItem(Guid.NewGuid(), new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── CompleteOrder ────────────────────────────────────────────────────────────

        [Fact]
        public async Task CompleteOrder_ValidOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.CompleteOrder(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CompleteOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.CompleteOrder(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── ClaimGuestOrder ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ClaimGuestOrder_ValidRequest_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest { OrderId = OrderId, VerificationPass = "pass123456" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ClaimGuestOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest { OrderId = Guid.NewGuid(), VerificationPass = "pass123456" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── CancelOrder ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task CancelOrder_ValidOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.CancelOrder(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CancelOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.CancelOrder(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CancelOrder_AlreadyCancelled_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng đã được huỷ"));

            var result = await _controller.CancelOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_AdminRole_ReturnsAllOrders()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<SalesOrder> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _orderUseCase.Setup(x => x.List(null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByStatus_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<SalesOrder> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _orderUseCase.Setup(x => x.List("PENDING", null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("PENDING", null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var detail = new SalesOrderDetailResponse { Order = new SalesOrder { Id = OrderId, Status = IncuSmart.Core.Enums.OrderStatus.PENDING }, Items = [] };
            _orderUseCase.Setup(x => x.GetById(OrderId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<SalesOrderDetailResponse?>(detail));

            var result = await _controller.GetById(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<SalesOrderDetailResponse?>("Đơn hàng không tồn tại"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
