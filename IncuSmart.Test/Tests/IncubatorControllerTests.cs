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
    public class IncubatorControllerTests
    {
        private readonly Mock<IIncubatorUseCase> _incubatorUseCase = new();
        private readonly Mock<IAuditLogUseCase>  _auditLogUseCase  = new();
        private readonly IncubatorController     _controller;

        private static readonly Guid IncubatorId = Guid.NewGuid();

        private static readonly IncubatorResponse SampleIncubatorResponse = new()
        {
            Id           = IncubatorId,
            SerialNumber = "SN-20260001",
            Status       = IncuSmart.Core.Enums.IncubatorStatus.ACTIVE
        };

        public IncubatorControllerTests()
        {
            _controller = new IncubatorController(_incubatorUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo máy ấp mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _incubatorUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateIncubatorCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<List<Guid>>([IncubatorId]));

            var result = await _controller.Create(new CreateIncubatorRequest
            {
                ModelId  = Guid.NewGuid(),
                Quantity = 1
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Model không tồn tại → 404
        public async Task Create_ModelNotFound_Returns404()
        {
            _incubatorUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateIncubatorCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<Guid>>("Model không tồn tại"));

            var result = await _controller.Create(new CreateIncubatorRequest
            {
                ModelId  = Guid.NewGuid(),
                Quantity = 1
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F01-TC03: Serial number đã tồn tại → 409
        public async Task Create_DuplicateSerialNumber_Returns409()
        {
            _incubatorUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateIncubatorCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<List<Guid>>("Serial number đã tồn tại"));

            var result = await _controller.Create(new CreateIncubatorRequest
            {
                ModelId  = Guid.NewGuid(),
                Quantity = 2
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: List ────────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Admin lấy tất cả máy ấp không lọc → 200
        public async Task List_AdminRole_ReturnsAllIncubators()
        {
            var paged = new PagedResult<IncubatorResponse>
            {
                Items      = [SampleIncubatorResponse],
                Page       = 1,
                PageSize   = 10,
                TotalItems = 1,
                TotalPages = 1
            };
            _incubatorUseCase.Setup(x => x.List(ControllerTestBase.AdminId, "ADMIN", null, null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Lọc theo trạng thái ACTIVE → 200
        public async Task List_FilterByStatus_Returns200()
        {
            var paged = new PagedResult<IncubatorResponse> { Items = [SampleIncubatorResponse], Page = 1, PageSize = 10, TotalItems = 1, TotalPages = 1 };
            _incubatorUseCase.Setup(x => x.List(ControllerTestBase.AdminId, "ADMIN", "ACTIVE", null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("ACTIVE", null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F03: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy chi tiết máy ấp tồn tại → 200
        public async Task GetById_ExistingIncubator_Returns200()
        {
            _incubatorUseCase.Setup(x => x.GetById(IncubatorId, ControllerTestBase.AdminId, "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<IncubatorResponse?>(SampleIncubatorResponse));

            var result = await _controller.GetById(IncubatorId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Máy ấp không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _incubatorUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<IncubatorResponse?>("Không tìm thấy máy ấp"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F04: UpdateConfigInstances ───────────────────────────────────────────────

        [Fact] // F04-TC01: Cập nhật config instances hợp lệ → 200
        public async Task UpdateConfigInstances_ValidRequest_Returns200()
        {
            _incubatorUseCase.Setup(x => x.UpdateConfigInstances(It.IsAny<IncuSmart.Core.Commands.UpdateConfigInstancesCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateConfigInstances(IncubatorId, new UpdateConfigInstancesRequest
            {
                Items = [new UpdateConfigInstanceItemRequest { ConfigInstanceId = Guid.NewGuid(), TargetValue = 37.5m }]
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Máy ấp không tồn tại → 404
        public async Task UpdateConfigInstances_IncubatorNotFound_Returns404()
        {
            _incubatorUseCase.Setup(x => x.UpdateConfigInstances(It.IsAny<IncuSmart.Core.Commands.UpdateConfigInstancesCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy máy ấp"));

            var result = await _controller.UpdateConfigInstances(Guid.NewGuid(), new UpdateConfigInstancesRequest { Items = [] });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F05: UpdateStatus ────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Cập nhật trạng thái hợp lệ → 200
        public async Task UpdateStatus_ValidRequest_Returns200()
        {
            _incubatorUseCase.Setup(x => x.UpdateStatus(IncubatorId, "INACTIVE", It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateStatus(IncubatorId, new UpdateIncubatorStatusRequest { Status = "INACTIVE" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Máy ấp không tồn tại → 404
        public async Task UpdateStatus_IncubatorNotFound_Returns404()
        {
            _incubatorUseCase.Setup(x => x.UpdateStatus(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy máy ấp"));

            var result = await _controller.UpdateStatus(Guid.NewGuid(), new UpdateIncubatorStatusRequest { Status = "INACTIVE" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F06: GetHatchingSeasons ──────────────────────────────────────────────────

        [Fact] // F06-TC01: Lấy danh sách mùa ấp của máy ấp tồn tại → 200
        public async Task GetHatchingSeasons_ExistingIncubator_Returns200()
        {
            _incubatorUseCase.Setup(x => x.GetHatchingSeasons(IncubatorId, ControllerTestBase.AdminId, "ADMIN", null, null))
                .ReturnsAsync(ControllerTestBase.OkResult<List<HatchingSeason>>([]));

            var result = await _controller.GetHatchingSeasons(IncubatorId, null, null);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
