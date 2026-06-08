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
    public class IncubatorModelControllerTests
    {
        private readonly Mock<IIncubatorModelUseCase> _modelUseCase    = new();
        private readonly Mock<IAuditLogUseCase>       _auditLogUseCase = new();
        private readonly IncubatorModelController     _controller;

        private static readonly Guid ModelId = Guid.NewGuid();

        private static readonly IncubatorModel SampleIncubatorModel = new()
        {
            Id        = ModelId,
            ModelCode = "XL-500",
            Name      = "Máy Ấp Trứng XL-500",
            UnitPrice = 5_000_000,
            Status    = IncuSmart.Core.Enums.BaseStatus.ACTIVE
        };

        private static readonly IncubatorModelDetailResponse SampleIncubatorModelDetail = new()
        {
            Id        = ModelId,
            ModelCode = "XL-500",
            Name      = "Máy Ấp Trứng XL-500",
            UnitPrice = 5_000_000,
            Status    = "ACTIVE",
            Configs   = []
        };

        public IncubatorModelControllerTests()
        {
            _controller = new IncubatorModelController(_modelUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo model máy ấp mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _modelUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(ModelId));

            var result = await _controller.Create(new CreateIncubatorModelRequest
            {
                ModelCode   = "XL-500",
                Name        = "Máy Ấp Trứng XL-500",
                Description = "Dung tích 500 trứng",
                UnitPrice   = 5_000_000,
                Configs     = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Tên hoặc mã model đã tồn tại → 409
        public async Task Create_DuplicateName_Returns409()
        {
            _modelUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Tên model đã tồn tại"));

            var result = await _controller.Create(new CreateIncubatorModelRequest
            {
                ModelCode = "XL-500",
                Name      = "Máy Ấp Trứng XL-500",
                UnitPrice = 5_000_000,
                Configs   = []
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: ListPublic ──────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy danh sách model public (ACTIVE) → 200
        public async Task ListPublic_Returns200()
        {
            var paged = new PagedResult<IncubatorModel>
            {
                Items      = [SampleIncubatorModel],
                Page       = 1,
                PageSize   = 12,
                TotalItems = 1,
                TotalPages = 1
            };
            _modelUseCase.Setup(x => x.List("ACTIVE", null, 1, 12))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.ListPublic(null, 1, 12);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F03: List ────────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<IncubatorModel>
            {
                Items      = [SampleIncubatorModel],
                Page       = 1,
                PageSize   = 20,
                TotalItems = 1,
                TotalPages = 1
            };
            _modelUseCase.Setup(x => x.List(null, null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Lọc theo trạng thái INACTIVE → 200
        public async Task List_FilterByStatus_Returns200()
        {
            var paged = new PagedResult<IncubatorModel> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _modelUseCase.Setup(x => x.List("INACTIVE", null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("INACTIVE", null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F04: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Lấy chi tiết model tồn tại → 200
        public async Task GetById_ExistingModel_Returns200()
        {
            _modelUseCase.Setup(x => x.GetById(ModelId))
                .ReturnsAsync(ControllerTestBase.OkResult<IncubatorModelDetailResponse?>(SampleIncubatorModelDetail));

            var result = await _controller.GetById(ModelId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Model không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _modelUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<IncubatorModelDetailResponse?>("Không tìm thấy model"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F05: Update ──────────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Cập nhật model hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _modelUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(ModelId, new UpdateIncubatorModelRequest
            {
                Name      = "Máy Ấp Trứng XL-500 Pro",
                UnitPrice = 6_000_000,
                Configs   = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Model không tồn tại → 404
        public async Task Update_ModelNotFound_Returns404()
        {
            _modelUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy model"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateIncubatorModelRequest { Name = "X", UnitPrice = 1, Configs = [] });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F06: Delete ──────────────────────────────────────────────────────────────

        [Fact] // F06-TC01: Xóa model tồn tại → 200
        public async Task Delete_ExistingModel_Returns200()
        {
            _modelUseCase.Setup(x => x.Delete(ModelId))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Delete(ModelId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F06-TC02: Model không tồn tại → 404
        public async Task Delete_ModelNotFound_Returns404()
        {
            _modelUseCase.Setup(x => x.Delete(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy model"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
