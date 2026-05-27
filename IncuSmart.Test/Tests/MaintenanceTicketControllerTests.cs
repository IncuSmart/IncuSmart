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
    public class MaintenanceTicketControllerTests
    {
        private readonly Mock<IMaintenanceTicketUseCase> _ticketUseCase   = new();
        private readonly Mock<IAuditLogUseCase>          _auditLogUseCase = new();
        private readonly MaintenanceTicketController     _controller;

        private static readonly Guid TicketId = Guid.NewGuid();

        public MaintenanceTicketControllerTests()
        {
            _controller = new MaintenanceTicketController(_ticketUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidRequest_Returns200()
        {
            _ticketUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateMaintenanceTicketCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(TicketId));

            var result = await _controller.Create(new CreateMaintenanceTicketRequest
            {
                IncubatorId      = Guid.NewGuid(),
                IssueDescription = "Máy bơm bị rò rỉ - Sửa máy bơm"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_IncubatorNotFound_Returns404()
        {
            _ticketUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateMaintenanceTicketCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy máy ấp"));

            var result = await _controller.Create(new CreateMaintenanceTicketRequest
            {
                IncubatorId      = Guid.NewGuid(),
                IssueDescription = "Báo hỏng - Mô tả lỗi"
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingTicket_Returns200()
        {
            var detail = new MaintenanceTicketDetailResponse
            {
                Ticket = new MaintenanceTicket { Id = TicketId, IssueDescription = "Sửa máy", IncubatorId = Guid.NewGuid(), Status = IncuSmart.Core.Enums.MaintenanceTicketStatus.PENDING },
                Logs   = []
            };
            _ticketUseCase.Setup(x => x.GetById(TicketId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<MaintenanceTicketDetailResponse?>(detail));

            var result = await _controller.GetById(TicketId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _ticketUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<MaintenanceTicketDetailResponse?>("Không tìm thấy ticket"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<MaintenanceTicket> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _ticketUseCase.Setup(x => x.List(null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByStatus_Returns200()
        {
            var paged = new PagedResult<MaintenanceTicket> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _ticketUseCase.Setup(x => x.List(null, null, "PENDING", It.IsAny<Guid?>(), "ADMIN", 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, "PENDING", 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── Assign ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Assign_ValidRequest_Returns200()
        {
            _ticketUseCase.Setup(x => x.Assign(It.IsAny<IncuSmart.Core.Commands.AssignMaintenanceTicketCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Assign(TicketId, new AssignMaintenanceTicketRequest { TechnicianId = ControllerTestBase.TechId });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Assign_TicketNotFound_Returns404()
        {
            _ticketUseCase.Setup(x => x.Assign(It.IsAny<IncuSmart.Core.Commands.AssignMaintenanceTicketCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy ticket"));

            var result = await _controller.Assign(Guid.NewGuid(), new AssignMaintenanceTicketRequest { TechnicianId = ControllerTestBase.TechId });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── UpdateStatus ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatus_ValidTransition_Returns200()
        {
            _ticketUseCase.Setup(x => x.UpdateStatus(It.IsAny<IncuSmart.Core.Commands.UpdateMaintenanceTicketStatusCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateStatus(TicketId, new UpdateMaintenanceTicketStatusRequest { Status = "IN_PROGRESS" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateStatus_InvalidTransition_Returns400()
        {
            _ticketUseCase.Setup(x => x.UpdateStatus(It.IsAny<IncuSmart.Core.Commands.UpdateMaintenanceTicketStatusCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Không thể chuyển trạng thái"));

            var result = await _controller.UpdateStatus(TicketId, new UpdateMaintenanceTicketStatusRequest { Status = "PENDING" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── Cancel ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Cancel_ValidTicket_Returns200()
        {
            _ticketUseCase.Setup(x => x.Cancel(TicketId, It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Cancel(TicketId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Cancel_TicketNotFound_Returns404()
        {
            _ticketUseCase.Setup(x => x.Cancel(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy ticket"));

            var result = await _controller.Cancel(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── AddLog ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddLog_ValidRequest_Returns200()
        {
            _ticketUseCase.Setup(x => x.AddLog(It.IsAny<IncuSmart.Core.Commands.CreateMaintenanceLogCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));

            var result = await _controller.AddLog(TicketId, new CreateMaintenanceLogRequest { Description = "Đã thay linh kiện" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task AddLog_TicketNotFound_Returns404()
        {
            _ticketUseCase.Setup(x => x.AddLog(It.IsAny<IncuSmart.Core.Commands.CreateMaintenanceLogCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy ticket"));

            var result = await _controller.AddLog(Guid.NewGuid(), new CreateMaintenanceLogRequest { Description = "Log" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── GetLogs ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetLogs_ExistingTicket_Returns200()
        {
            _ticketUseCase.Setup(x => x.GetLogs(TicketId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<MaintenanceLog>>([]));

            var result = await _controller.GetLogs(TicketId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetLogs_TicketNotFound_Returns404()
        {
            _ticketUseCase.Setup(x => x.GetLogs(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<MaintenanceLog>>("Không tìm thấy ticket"));

            var result = await _controller.GetLogs(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
