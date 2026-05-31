using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class AlertControllerTests
    {
        private readonly Mock<IAlertUseCase>    _alertUseCase    = new();
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly AlertController        _controller;

        private static readonly Guid AlertId     = Guid.NewGuid();
        private static readonly Guid IncubatorId = Guid.NewGuid();

        private static readonly Alert SampleAlert = new()
        {
            Id          = AlertId,
            Message     = "Nhiệt độ vượt ngưỡng 42°C",
            Status      = IncuSmart.Core.Enums.AlertStatus.OPEN,
            IncubatorId = IncubatorId
        };

        public AlertControllerTests()
        {
            _controller = new AlertController(_alertUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── F01: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Lấy chi tiết cảnh báo tồn tại → 200
        public async Task GetById_ExistingAlert_Returns200()
        {
            _alertUseCase.Setup(x => x.GetById(AlertId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<Alert?>(SampleAlert));

            var result = await _controller.GetById(AlertId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Cảnh báo không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _alertUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Alert?>("Không tìm thấy cảnh báo"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F01-TC03: Customer xem cảnh báo máy của người khác → 403
        public async Task GetById_Forbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _alertUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<Alert?>("Không có quyền truy cập"));

            var result = await _controller.GetById(Guid.NewGuid());

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        // ─── F02: List ────────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<Alert> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _alertUseCase.Setup(x => x.List(null, null, null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Lọc theo mức độ HIGH → 200
        public async Task List_FilterBySeverity_Returns200()
        {
            var paged = new PagedResult<Alert> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _alertUseCase.Setup(x => x.List(null, "HIGH", null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, "HIGH", null, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC03: Lọc theo khoảng thời gian → 200
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

        // ─── F03: Resolve ─────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Xử lý cảnh báo hợp lệ → 200
        public async Task Resolve_ValidAlert_Returns200()
        {
            _alertUseCase.Setup(x => x.Resolve(It.IsAny<IncuSmart.Core.Commands.ResolveAlertCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Resolve(AlertId, new ResolveAlertRequest { Message = "Đã điều chỉnh nhiệt độ về mức an toàn" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Cảnh báo không tồn tại → 404
        public async Task Resolve_AlertNotFound_Returns404()
        {
            _alertUseCase.Setup(x => x.Resolve(It.IsAny<IncuSmart.Core.Commands.ResolveAlertCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy cảnh báo"));

            var result = await _controller.Resolve(Guid.NewGuid(), new ResolveAlertRequest { Message = "Ghi chú" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F03-TC03: Cảnh báo đã được xử lý trước đó → 400
        public async Task Resolve_AlreadyResolved_Returns400()
        {
            _alertUseCase.Setup(x => x.Resolve(It.IsAny<IncuSmart.Core.Commands.ResolveAlertCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Cảnh báo đã được xử lý"));

            var result = await _controller.Resolve(AlertId, new ResolveAlertRequest { Message = "Ghi chú" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
