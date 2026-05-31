using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class AuditLogControllerTests
    {
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly AuditLogController     _controller;

        private static readonly Guid SampleLogId = Guid.NewGuid();

        private static readonly AuditLog SampleAuditLog = new()
        {
            Id     = SampleLogId,
            Action = IncuSmart.Core.Enums.AuditAction.CREATE,
            Entity = IncuSmart.Core.Enums.AuditEntityType.USER,
            UserId = ControllerTestBase.AdminId,
            Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE
        };

        public AuditLogControllerTests()
        {
            _controller = new AuditLogController(_auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── F01: List ────────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<AuditLog>
            {
                Items      = [SampleAuditLog],
                Page       = 1,
                PageSize   = 10,
                TotalItems = 1,
                TotalPages = 1
            };
            _auditLogUseCase.Setup(x => x.List(null, null, null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Lọc theo userId → 200
        public async Task List_FilterByUserId_Returns200()
        {
            var paged = new PagedResult<AuditLog> { Items = [SampleAuditLog], Page = 1, PageSize = 10, TotalItems = 1, TotalPages = 1 };
            _auditLogUseCase.Setup(x => x.List(ControllerTestBase.AdminId, null, null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(ControllerTestBase.AdminId, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC03: Lọc theo action CREATE → 200
        public async Task List_FilterByAction_Returns200()
        {
            var paged = new PagedResult<AuditLog> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _auditLogUseCase.Setup(x => x.List(null, "CREATE", null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, "CREATE", null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC04: Lọc theo entity USER → 200
        public async Task List_FilterByEntity_Returns200()
        {
            var paged = new PagedResult<AuditLog> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _auditLogUseCase.Setup(x => x.List(null, null, "USER", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, "USER", new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F02: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy chi tiết audit log tồn tại → 200
        public async Task GetById_ExistingLog_Returns200()
        {
            _auditLogUseCase.Setup(x => x.GetById(SampleLogId))
                .ReturnsAsync(ControllerTestBase.OkResult<AuditLog?>(SampleAuditLog));

            var result = await _controller.GetById(SampleLogId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Audit log không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _auditLogUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<AuditLog?>("Không tìm thấy audit log"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
