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

        public CustomerControllerTests()
        {
            _controller = new CustomerController(_customerUseCase.Object, _customerRepo.Object);
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<CustomerSummaryResponse>
            {
                Items      = [new CustomerSummaryResponse { Id = Guid.NewGuid(), FullName = "Khách A", Phone = "090", UserStatus = "ACTIVE", CustomerStatus = "ACTIVE", Role = "CUSTOMER" }],
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

        [Fact]
        public async Task List_FilterByStatus_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var paged = new PagedResult<CustomerSummaryResponse> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _customerUseCase.Setup(x => x.List("ACTIVE", null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("ACTIVE", null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingCustomer_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var customerId = Guid.NewGuid();
            var detail     = new CustomerDetailResponse { Id = customerId, FullName = "Khách B", Phone = "090", UserStatus = "ACTIVE", CustomerStatus = "ACTIVE", Role = "CUSTOMER" };
            _customerUseCase.Setup(x => x.GetById(customerId))
                .ReturnsAsync(ControllerTestBase.OkResult<CustomerDetailResponse?>(detail));

            var result = await _controller.GetById(customerId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<CustomerDetailResponse?>("Không tìm thấy khách hàng"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── GetMe ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetMe_AuthenticatedCustomer_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            var detail = new CustomerDetailResponse { Id = Guid.NewGuid(), UserId = ControllerTestBase.CustomerId, FullName = "Khách C", Phone = "090", UserStatus = "ACTIVE", CustomerStatus = "ACTIVE", Role = "CUSTOMER" };
            _customerUseCase.Setup(x => x.GetByUserId(ControllerTestBase.CustomerId))
                .ReturnsAsync(ControllerTestBase.OkResult<CustomerDetailResponse?>(detail));

            var result = await _controller.GetMe();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetOrders ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetOrders_ExistingCustomer_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var customerId = Guid.NewGuid();
            _customerUseCase.Setup(x => x.GetOrders(customerId))
                .ReturnsAsync(ControllerTestBase.OkResult<List<SalesOrder>>([]));

            var result = await _controller.GetOrders(customerId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetOrders_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.GetOrders(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<SalesOrder>>("Không tìm thấy khách hàng"));

            var result = await _controller.GetOrders(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Update (Admin/Sales) ─────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            var customerId = Guid.NewGuid();
            _customerUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateCustomerProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(customerId, new UpdateCustomerProfileRequest { Address = "123 Đường ABC" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
            _customerUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateCustomerProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy khách hàng"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateCustomerProfileRequest { Address = "Địa chỉ" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── UpdateMe ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateMe_CustomerFound_Returns200()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            var customer = new Customer { Id = Guid.NewGuid(), UserId = ControllerTestBase.CustomerId, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE };
            _customerRepo.Setup(x => x.FindByUserId(ControllerTestBase.CustomerId))
                .ReturnsAsync(customer);
            _customerUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateCustomerProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateMe(new UpdateCustomerProfileRequest { Address = "456 Đường DEF" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateMe_CustomerNotFound_Returns404()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _customerRepo.Setup(x => x.FindByUserId(ControllerTestBase.CustomerId))
                .ReturnsAsync((Customer?)null);

            var result = await _controller.UpdateMe(new UpdateCustomerProfileRequest { Address = "Địa chỉ" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
