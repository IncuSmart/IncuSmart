using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class AlertControllerTests
    {
        private readonly Mock<IAlertUseCase>    _alertUseCase    = new();
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly AlertController        _controller;

        public AlertControllerTests()
        {
            _controller = new AlertController(_alertUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingAlert_Returns200()
        {
            var alertId = Guid.NewGuid();
            var alert   = new Alert { Id = alertId, Message = "Nhiệt độ vượt ngưỡng", Status = IncuSmart.Core.Enums.AlertStatus.OPEN, IncubatorId = Guid.NewGuid() };
            _alertUseCase.Setup(x => x.GetById(alertId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<Alert?>(alert));

            var result = await _controller.GetById(alertId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _alertUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Alert?>("Không tìm thấy cảnh báo"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetById_Forbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _alertUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(new IncuSmart.Core.ResultModel<Alert?> { StatusCode = "403", Message = "Không có quyền truy cập" });

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<Alert> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _alertUseCase.Setup(x => x.List(null, null, null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterBySeverity_Returns200()
        {
            var paged = new PagedResult<Alert> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _alertUseCase.Setup(x => x.List(null, "HIGH", null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, "HIGH", null, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByDateRange_Returns200()
        {
            var from  = DateTime.UtcNow.AddDays(-7);
            var to    = DateTime.UtcNow;
            var paged = new PagedResult<Alert> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _alertUseCase.Setup(x => x.List(null, null, null, from, to, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, from, to, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── Resolve ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Resolve_ValidAlert_Returns200()
        {
            var alertId = Guid.NewGuid();
            _alertUseCase.Setup(x => x.Resolve(It.IsAny<IncuSmart.Core.Commands.ResolveAlertCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Resolve(alertId, new ResolveAlertRequest { Message = "Đã xử lý sự cố" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Resolve_AlertNotFound_Returns404()
        {
            _alertUseCase.Setup(x => x.Resolve(It.IsAny<IncuSmart.Core.Commands.ResolveAlertCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy cảnh báo"));

            var result = await _controller.Resolve(Guid.NewGuid(), new ResolveAlertRequest { Message = "Ghi chú" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Resolve_AlreadyResolved_Returns400()
        {
            _alertUseCase.Setup(x => x.Resolve(It.IsAny<IncuSmart.Core.Commands.ResolveAlertCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Cảnh báo đã được xử lý"));

            var result = await _controller.Resolve(Guid.NewGuid(), new ResolveAlertRequest { Message = "Ghi chú" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
