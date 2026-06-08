using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Http;
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

        // ─── F01: CreateOrderByCustomer ───────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo đơn thành công với 1 sản phẩm
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

        [Fact] // F01-TC02: Tạo đơn thành công với nhiều sản phẩm
        public async Task CreateOrderByCustomer_MultipleItems_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<CreateOrderResponse?>(SampleOrderResponse));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest
            {
                Items =
                [
                    new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 2 },
                    new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }
                ]
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC03: Customer profile chưa được tạo → 404
        public async Task CreateOrderByCustomer_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<CreateOrderResponse?>("Khách hàng không tồn tại"));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest
            {
                Items = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F01-TC04: Items rỗng → 400
        public async Task CreateOrderByCustomer_EmptyItems_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Cần ít nhất 1 sản phẩm"));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest { Items = [] });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F01-TC05: Sản phẩm không hợp lệ (PrepareOrderItems trả 400) → 400
        public async Task CreateOrderByCustomer_InvalidProducts_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Sản phẩm không hợp lệ"));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest
            {
                Items = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F01-TC06: Lỗi hệ thống (payment gateway lỗi) → 500
        public async Task CreateOrderByCustomer_SystemError_Returns500()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CreateOrderByCustomer(It.IsAny<IncuSmart.Core.Commands.CreateOrderByCustomerCommand>()))
                .ReturnsAsync(ControllerTestBase.ErrorResult<CreateOrderResponse?>("Lỗi hệ thống"));

            var result = await _controller.CreateOrderByCustomer(new CreateOrderByCustomerRequest
            {
                Items = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        // ─── F02: CreateOrderByGuest ──────────────────────────────────────────────────

        [Fact] // F02-TC01: Khách vãng lai tạo đơn thành công
        public async Task CreateOrderByGuest_ValidRequest_Returns200()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<CreateOrderResponse?>(SampleOrderResponse));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Nguyễn Văn A",
                Phone            = "0901234567",
                Email            = "a@test.com",
                Items            = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Items rỗng → 400
        public async Task CreateOrderByGuest_EmptyItems_Returns400()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Cần ít nhất 1 sản phẩm"));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Khách",
                Phone            = "0901234567",
                Email            = "khach@test.com",
                Items            = []
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F02-TC03: Sản phẩm không hợp lệ → 400 (FIX: test cũ sai – mock 404, thực tế PrepareOrderItems trả 400)
        public async Task CreateOrderByGuest_InvalidProducts_Returns400()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Sản phẩm không hợp lệ"));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Khách",
                Phone            = "0901234567",
                Email            = "khach@test.com",
                Items            =[new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F02-TC04: Số lượng = 0 → 400
        public async Task CreateOrderByGuest_ZeroQuantity_Returns400()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Số lượng phải lớn hơn 0"));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Khách",
                Phone            = "0901234567",
                Email            = "khach@test.com",
                Items            =[new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 0 }]
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F02-TC05: Lỗi hệ thống → 500
        public async Task CreateOrderByGuest_SystemError_Returns500()
        {
            _orderUseCase.Setup(x => x.CreateOrderByGuest(It.IsAny<IncuSmart.Core.Commands.CreateOrderByGuestCommand>()))
                .ReturnsAsync(ControllerTestBase.ErrorResult<CreateOrderResponse?>("Lỗi hệ thống"));

            var result = await _controller.CreateOrderByGuest(new CreateOrderByGuestRequest
            {
                FullName         = "Khách",
                Phone            = "0901234567",
                Email            = "khach@test.com",
                Items            =[new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        // ─── F03: CreateOrderBySales ──────────────────────────────────────────────────

        [Fact] // F03-TC01: Staff tạo đơn cho customer thành công
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

        [Fact] // F03-TC02: CustomerId không tồn tại → 404
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

        [Fact] // F03-TC03: Items rỗng → 400
        public async Task CreateOrderBySales_EmptyItems_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CreateOrderBySales(It.IsAny<IncuSmart.Core.Commands.CreateOrderBySalesCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Cần ít nhất 1 sản phẩm"));

            var result = await _controller.CreateOrderBySales(new CreateOrderBySalesRequest
            {
                CustomerId = ControllerTestBase.CustomerId,
                Items      = []
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F03-TC04: Sản phẩm inactive → 400
        public async Task CreateOrderBySales_InactiveProducts_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CreateOrderBySales(It.IsAny<IncuSmart.Core.Commands.CreateOrderBySalesCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<CreateOrderResponse?>("Sản phẩm không còn được bán"));

            var result = await _controller.CreateOrderBySales(new CreateOrderBySalesRequest
            {
                CustomerId = ControllerTestBase.CustomerId,
                Items      = [new OrderItemRequest { IncubatorModelId = Guid.NewGuid(), Quantity = 1 }]
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── F04: AssignIncubatorToOrderItem ──────────────────────────────────────────

        [Fact] // F04-TC01: Gán incubator thành công
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

        [Fact] // F04-TC02: Order không tồn tại → 404
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

        [Fact] // F04-TC03: Order đã huỷ hoặc hoàn tất → 400
        public async Task AssignIncubatorToOrderItem_OrderCancelled_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng không thể cập nhật"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F04-TC04: Order chưa thanh toán → 400
        public async Task AssignIncubatorToOrderItem_PaymentNotCompleted_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng chưa được thanh toán"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F04-TC05: Order item không thuộc về order hoặc không tồn tại → 404
        public async Task AssignIncubatorToOrderItem_OrderItemNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy order item"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F04-TC06: Order item đã được gán (status != PENDING_ASSIGNMENT) → 400
        public async Task AssignIncubatorToOrderItem_OrderItemAlreadyAssigned_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Order item đã được gán"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F04-TC07: Incubator không tồn tại → 404
        public async Task AssignIncubatorToOrderItem_IncubatorNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy incubator"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F04-TC08: Model của incubator không khớp với order item → 400
        public async Task AssignIncubatorToOrderItem_IncubatorModelMismatch_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Model không khớp với order item"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F04-TC09: Incubator không khả dụng (RESERVED/ACTIVE/có customer) → 400
        public async Task AssignIncubatorToOrderItem_IncubatorNotAvailable_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.AssignIncubatorToOrderItem(It.IsAny<IncuSmart.Core.Commands.AssignIncubatorToOrderItemCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Incubator không khả dụng"));

            var result = await _controller.AssignIncubatorToOrderItem(OrderId, new AssignIncubatorToOrderItemRequest
            {
                OrderItemId = Guid.NewGuid(),
                IncubatorId = Guid.NewGuid()
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── F05: ShipOrder ───────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Ship order thành công
        public async Task ShipOrder_ValidOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.ShipOrder(It.IsAny<IncuSmart.Core.Commands.ShipOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ShipOrder(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Order không tồn tại → 404
        public async Task ShipOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.ShipOrder(It.IsAny<IncuSmart.Core.Commands.ShipOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.ShipOrder(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F05-TC03: Order chưa thanh toán → 400
        public async Task ShipOrder_PaymentNotCompleted_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.ShipOrder(It.IsAny<IncuSmart.Core.Commands.ShipOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng chưa được thanh toán"));

            var result = await _controller.ShipOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F05-TC04: Order không ở trạng thái PROCESSING → 400
        public async Task ShipOrder_OrderNotProcessing_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.ShipOrder(It.IsAny<IncuSmart.Core.Commands.ShipOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng không thể cập nhật"));

            var result = await _controller.ShipOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F05-TC05: Chưa tất cả items được gán incubator → 400
        public async Task ShipOrder_NotAllItemsAssigned_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.ShipOrder(It.IsAny<IncuSmart.Core.Commands.ShipOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Chưa gán đủ incubator cho tất cả order items"));

            var result = await _controller.ShipOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── F06: CompleteOrder ───────────────────────────────────────────────────────

        [Fact] // F06-TC01: Staff hoàn tất đơn thành công
        public async Task CompleteOrder_ValidOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.CompleteOrder(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F06-TC02: Order không tồn tại → 404
        public async Task CompleteOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.CompleteOrder(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F06-TC03: Order đã bị huỷ → 400
        public async Task CompleteOrder_OrderCancelled_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng đã bị huỷ không thể hoàn tất"));

            var result = await _controller.CompleteOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F06-TC04: Order đã hoàn tất trước đó → 400
        public async Task CompleteOrder_OrderAlreadyCompleted_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng đã hoàn tất"));

            var result = await _controller.CompleteOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F06-TC05: Order không ở trạng thái SHIPPING → 400
        public async Task CompleteOrder_OrderNotShipping_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "SALES_STAFF");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng chưa ở trạng thái SHIPPING"));

            var result = await _controller.CompleteOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F06-TC06: Customer cố hoàn tất đơn của người khác → 403
        public async Task CompleteOrder_CustomerAccessDenied_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CompleteOrder(It.IsAny<IncuSmart.Core.Commands.CompleteOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<bool>("Không có quyền thực hiện thao tác này"));

            var result = await _controller.CompleteOrder(OrderId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        // ─── F07: ClaimGuestOrder ─────────────────────────────────────────────────────

        [Fact] // F07-TC01: Claim guest order thành công
        public async Task ClaimGuestOrder_ValidRequest_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest
            {
                OrderCode        = "SO-20260527-0001",
                VerificationPass = "pass123456"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F07-TC02: Order code không tồn tại → 404
        public async Task ClaimGuestOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest
            {
                OrderCode        = "SO-NOTFOUND-0000",
                VerificationPass = "pass123456"
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F07-TC03: Customer profile không tồn tại → 404
        public async Task ClaimGuestOrder_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Khách hàng không tồn tại"));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest
            {
                OrderCode        = "SO-20260527-0001",
                VerificationPass = "pass123456"
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F07-TC04: Order đã được claim trước đó → 400
        public async Task ClaimGuestOrder_OrderAlreadyClaimed_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng đã được nhận"));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest
            {
                OrderCode        = "SO-CLAIMED-0001",
                VerificationPass = "pass123456"
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F07-TC05: Order chưa hoàn tất (status != COMPLETED) → 400
        public async Task ClaimGuestOrder_OrderNotCompleted_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Chỉ có thể nhận đơn hàng đã hoàn tất"));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest
            {
                OrderCode        = "SO-PENDING-0001",
                VerificationPass = "pass123456"
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F07-TC06: Mật khẩu xác thực sai → 400
        public async Task ClaimGuestOrder_WrongVerificationPass_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.ClaimGuestOrder(It.IsAny<IncuSmart.Core.Commands.ClaimGuestOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Mật khẩu xác thực không đúng"));

            var result = await _controller.ClaimGuestOrder(new ClaimGuestOrderRequest
            {
                OrderCode        = "SO-20260527-0001",
                VerificationPass = "wrong_pass"
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── F08: CancelOrder ─────────────────────────────────────────────────────────

        [Fact] // F08-TC01: Admin huỷ đơn thành công
        public async Task CancelOrder_AdminCancelsOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.CancelOrder(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F08-TC02: Customer huỷ đơn của mình thành công
        public async Task CancelOrder_CustomerCancelsOwnOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.CancelOrder(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F08-TC03: Order không tồn tại → 404
        public async Task CancelOrder_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Đơn hàng không tồn tại"));

            var result = await _controller.CancelOrder(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F08-TC04: Order đã bị huỷ rồi → 400
        public async Task CancelOrder_AlreadyCancelled_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Đơn hàng đã được huỷ"));

            var result = await _controller.CancelOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F08-TC05: Order đã hoàn tất (COMPLETED) → 400
        public async Task CancelOrder_OrderCompleted_Returns400()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Không thể huỷ đơn hàng đã hoàn tất"));

            var result = await _controller.CancelOrder(OrderId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact] // F08-TC06: Customer cố huỷ đơn của người khác → 403
        public async Task CancelOrder_CustomerAccessDenied_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.CancelOrder(It.IsAny<IncuSmart.Core.Commands.CancelOrderCommand>()))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<bool>("Không có quyền thực hiện thao tác này"));

            var result = await _controller.CancelOrder(OrderId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        // ─── F09: List ────────────────────────────────────────────────────────────────

        [Fact] // F09-TC01: Admin lấy tất cả đơn không lọc
        public async Task List_AdminRole_ReturnsAllOrders()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<SalesOrder> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _orderUseCase.Setup(x => x.List(null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F09-TC02: Admin lọc theo status
        public async Task List_FilterByStatus_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<SalesOrder> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _orderUseCase.Setup(x => x.List("PENDING", null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("PENDING", null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F09-TC03: Admin lọc theo customerId
        public async Task List_FilterByCustomerId_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var specificCustomerId = Guid.NewGuid();
            var paged = new PagedResult<SalesOrder> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _orderUseCase.Setup(x => x.List(null, specificCustomerId, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, specificCustomerId, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F09-TC04: Customer chỉ xem đơn của mình
        public async Task List_CustomerRole_ReturnsOwnOrders()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            var paged = new PagedResult<SalesOrder> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _orderUseCase.Setup(x => x.List(null, null, It.IsAny<Guid?>(), "CUSTOMER", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F10: GetPaymentStatus ────────────────────────────────────────────────────

        [Fact] // F10-TC01: Admin lấy payment status thành công
        public async Task GetPaymentStatus_AdminRole_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paymentStatus = new OrderPaymentStatusResponse
            {
                OrderId       = OrderId,
                OrderCode     = "SO-20260527-0001",
                PaymentStatus = IncuSmart.Core.Enums.PaymentStatus.PENDING,
                PaidAt        = null
            };
            _orderUseCase.Setup(x => x.GetPaymentStatus(OrderId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<OrderPaymentStatusResponse?>(paymentStatus));

            var result = await _controller.GetPaymentStatus(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F10-TC02: Order không tồn tại → 404
        public async Task GetPaymentStatus_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.GetPaymentStatus(It.IsAny<Guid>(), It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<OrderPaymentStatusResponse?>("Đơn hàng không tồn tại"));

            var result = await _controller.GetPaymentStatus(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F10-TC03: Customer xem payment status đơn của người khác → 403
        public async Task GetPaymentStatus_CustomerAccessDenied_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.GetPaymentStatus(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<OrderPaymentStatusResponse?>("Không có quyền truy cập"));

            var result = await _controller.GetPaymentStatus(OrderId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact] // F10-TC04: Customer xem payment status đơn của mình → 200
        public async Task GetPaymentStatus_CustomerOwnOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            var paymentStatus = new OrderPaymentStatusResponse
            {
                OrderId       = OrderId,
                OrderCode     = "SO-20260527-0001",
                PaymentStatus = IncuSmart.Core.Enums.PaymentStatus.PAID,
                PaidAt        = DateTime.UtcNow
            };
            _orderUseCase.Setup(x => x.GetPaymentStatus(OrderId, It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.OkResult<OrderPaymentStatusResponse?>(paymentStatus));

            var result = await _controller.GetPaymentStatus(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F11: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F11-TC01: Admin lấy chi tiết đơn thành công
        public async Task GetById_ExistingOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var detail = new SalesOrderDetailResponse
            {
                Order = new SalesOrder { Id = OrderId, Status = IncuSmart.Core.Enums.OrderStatus.PENDING },
                Items = []
            };
            _orderUseCase.Setup(x => x.GetById(OrderId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<SalesOrderDetailResponse?>(detail));

            var result = await _controller.GetById(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F11-TC02: Order không tồn tại → 404
        public async Task GetById_OrderNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _orderUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<SalesOrderDetailResponse?>("Đơn hàng không tồn tại"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F11-TC03: Customer xem chi tiết đơn của người khác → 403
        public async Task GetById_CustomerAccessDenied_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _orderUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<SalesOrderDetailResponse?>("Không có quyền truy cập"));

            var result = await _controller.GetById(OrderId);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact] // F11-TC04: Customer xem chi tiết đơn của mình → 200
        public async Task GetById_CustomerOwnOrder_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            var detail = new SalesOrderDetailResponse
            {
                Order = new SalesOrder { Id = OrderId, Status = IncuSmart.Core.Enums.OrderStatus.PENDING },
                Items = []
            };
            _orderUseCase.Setup(x => x.GetById(OrderId, It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.OkResult<SalesOrderDetailResponse?>(detail));

            var result = await _controller.GetById(OrderId);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
