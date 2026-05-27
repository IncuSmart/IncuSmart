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

        public AuditLogControllerTests()
        {
            _controller = new AuditLogController(_auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<AuditLog>
            {
                Items      = [new AuditLog { Id = Guid.NewGuid(), Action = IncuSmart.Core.Enums.AuditAction.CREATE, Entity = IncuSmart.Core.Enums.AuditEntityType.USER, UserId = ControllerTestBase.AdminId, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }],
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

        [Fact]
        public async Task List_FilterByUserId_Returns200()
        {
            var userId = ControllerTestBase.AdminId;
            var paged  = new PagedResult<AuditLog> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _auditLogUseCase.Setup(x => x.List(userId, null, null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(userId, null, null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByAction_Returns200()
        {
            var paged = new PagedResult<AuditLog> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _auditLogUseCase.Setup(x => x.List(null, "CREATE", null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, "CREATE", null, new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByEntity_Returns200()
        {
            var paged = new PagedResult<AuditLog> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _auditLogUseCase.Setup(x => x.List(null, null, "USER", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, "USER", new IncuSmart.API.Requests.PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingLog_Returns200()
        {
            var logId = Guid.NewGuid();
            var log   = new AuditLog { Id = logId, Action = IncuSmart.Core.Enums.AuditAction.UPDATE, Entity = IncuSmart.Core.Enums.AuditEntityType.INCUBATOR, UserId = ControllerTestBase.AdminId, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE };
            _auditLogUseCase.Setup(x => x.GetById(logId))
                .ReturnsAsync(ControllerTestBase.OkResult<AuditLog?>(log));

            var result = await _controller.GetById(logId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _auditLogUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<AuditLog?>("Không tìm thấy audit log"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
