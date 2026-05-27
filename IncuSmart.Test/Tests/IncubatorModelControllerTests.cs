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

        public IncubatorModelControllerTests()
        {
            _controller = new IncubatorModelController(_modelUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
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

        [Fact]
        public async Task Create_DuplicateName_Returns409()
        {
            _modelUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Tên model đã tồn tại"));

            var result = await _controller.Create(new CreateIncubatorModelRequest
            {
                ModelCode = "EXIST",
                Name      = "Model Đã Tồn Tại",
                UnitPrice = 1_000_000,
                Configs   = []
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── ListPublic ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task ListPublic_Returns200()
        {
            var paged = new PagedResult<IncubatorModel>
            {
                Items      = [new IncubatorModel { Id = ModelId, ModelCode = "XL-500", Name = "XL-500", UnitPrice = 5_000_000, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }],
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

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<IncubatorModel> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _modelUseCase.Setup(x => x.List(null, null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByStatus_Returns200()
        {
            var paged = new PagedResult<IncubatorModel> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _modelUseCase.Setup(x => x.List("INACTIVE", null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("INACTIVE", null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingModel_Returns200()
        {
            var model = new IncubatorModel { Id = ModelId, ModelCode = "XL-500", Name = "XL-500", UnitPrice = 5_000_000, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE };
            _modelUseCase.Setup(x => x.GetById(ModelId))
                .ReturnsAsync(ControllerTestBase.OkResult<IncubatorModel?>(model));

            var result = await _controller.GetById(ModelId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _modelUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<IncubatorModel?>("Không tìm thấy model"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Update ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            _modelUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(ModelId, new UpdateIncubatorModelRequest
            {
                Name      = "XL-500 Pro",
                UnitPrice = 6_000_000,
                Configs   = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_ModelNotFound_Returns404()
        {
            _modelUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateIncubatorModelCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy model"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateIncubatorModelRequest { Name = "X", UnitPrice = 1, Configs = [] });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Delete ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ExistingModel_Returns200()
        {
            _modelUseCase.Setup(x => x.Delete(ModelId))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Delete(ModelId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Delete_ModelNotFound_Returns404()
        {
            _modelUseCase.Setup(x => x.Delete(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy model"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
