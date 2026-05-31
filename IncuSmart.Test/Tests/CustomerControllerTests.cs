using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Ports.Outbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerUseCase>    _customerUseCase = new();
        private readonly Mock<ICustomerRepository> _customerRepo    = new();
        private readonly CustomerController        _controller;

        private static readonly Guid SampleCustomerId = Guid.NewGuid();

        private static readonly CustomerDetailResponse SampleCustomerDetail = new()
        {
            Id             = SampleCustomerId,
            UserId         = ControllerTestBase.CustomerId,
            FullName       = "Nguyễn Văn A",
            Phone          = "0901234567",
            UserStatus     = "ACTIVE",
            CustomerStatus = "ACTIVE",
            Role           = "CUSTOMER"
        };

        public CustomerControllerTests()
        {
            _controller = new CustomerController(_customerUseCase.Object, _customerRepo.Object);
        }

        // ─── F01: List ────────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<CustomerSummaryResponse>
            {
                Items      = [new CustomerSummaryResponse { Id = SampleCustomerId, FullName = "Nguyễn Văn A", Phone = "0901234567", UserStatus = "ACTIVE", CustomerStatus = "ACTIVE", Role = "CUSTOMER" }],
                Page       = 1,
                PageSize   = 10,
                TotalItems = 1,
                TotalPages = 1
            };
            _customerUseCase.Setup(x => x.List(null, null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Lọc theo trạng thái ACTIVE → 200
        public async Task List_FilterByStatus_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<CustomerSummaryResponse> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _customerUseCase.Setup(x => x.List("ACTIVE", null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("ACTIVE", null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F02: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy chi tiết khách hàng tồn tại → 200
        public async Task GetById_ExistingCustomer_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.GetById(SampleCustomerId))
                .ReturnsAsync(ControllerTestBase.OkResult<CustomerDetailResponse?>(SampleCustomerDetail));

            var result = await _controller.GetById(SampleCustomerId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Khách hàng không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<CustomerDetailResponse?>("Không tìm thấy khách hàng"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F03: GetMe ───────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy thông tin cá nhân của customer đã xác thực → 200
        public async Task GetMe_AuthenticatedCustomer_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _customerUseCase.Setup(x => x.GetByUserId(ControllerTestBase.CustomerId))
                .ReturnsAsync(ControllerTestBase.OkResult<CustomerDetailResponse?>(SampleCustomerDetail));

            var result = await _controller.GetMe();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F04: GetOrders ───────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Lấy đơn hàng của khách hàng tồn tại → 200
        public async Task GetOrders_ExistingCustomer_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.GetOrders(SampleCustomerId))
                .ReturnsAsync(ControllerTestBase.OkResult<List<SalesOrder>>([]));

            var result = await _controller.GetOrders(SampleCustomerId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Khách hàng không tồn tại → 404
        public async Task GetOrders_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.GetOrders(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<SalesOrder>>("Không tìm thấy khách hàng"));

            var result = await _controller.GetOrders(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F05: Update ──────────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Cập nhật thông tin hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateCustomerProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(SampleCustomerId, new UpdateCustomerProfileRequest { Address = "123 Đường ABC, Quận 1, TP.HCM" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Khách hàng không tồn tại → 404
        public async Task Update_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateCustomerProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy khách hàng"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateCustomerProfileRequest { Address = "Địa chỉ mới" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F06: UpdateMe ────────────────────────────────────────────────────────────

        [Fact] // F06-TC01: Customer cập nhật hồ sơ cá nhân → 200
        public async Task UpdateMe_CustomerFound_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            var customer = new Customer { Id = SampleCustomerId, UserId = ControllerTestBase.CustomerId, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE };
            _customerRepo.Setup(x => x.FindByUserId(ControllerTestBase.CustomerId))
                .ReturnsAsync(customer);
            _customerUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateCustomerProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateMe(new UpdateCustomerProfileRequest { Address = "456 Đường DEF, Quận 3, TP.HCM" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F06-TC02: Customer profile chưa được tạo → 404
        public async Task UpdateMe_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _customerRepo.Setup(x => x.FindByUserId(ControllerTestBase.CustomerId))
                .ReturnsAsync((Customer?)null);

            var result = await _controller.UpdateMe(new UpdateCustomerProfileRequest { Address = "Địa chỉ mới" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
