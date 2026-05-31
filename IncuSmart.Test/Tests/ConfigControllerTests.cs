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
    public class ConfigControllerTests
    {
        private readonly Mock<IConfigUseCase>   _configUseCase   = new();
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly ConfigController       _controller;

        private static readonly Guid ConfigId = Guid.NewGuid();

        private static readonly Config SampleConfig = new()
        {
            Id     = ConfigId,
            Name   = "Nhiệt độ",
            Type   = "TEMPERATURE",
            Unit   = "°C",
            Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE
        };

        public ConfigControllerTests()
        {
            _controller = new ConfigController(_configUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo config mới hợp lệ → 200
        public async Task Create_ValidConfig_Returns200()
        {
            _configUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateConfigCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(ConfigId));

            var result = await _controller.Create(new CreateConfigRequest
            {
                Name = "Nhiệt độ",
                Type = "TEMPERATURE",
                Unit = "°C"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Tên config đã tồn tại → 409
        public async Task Create_DuplicateName_Returns409()
        {
            _configUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateConfigCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Tên config đã tồn tại"));

            var result = await _controller.Create(new CreateConfigRequest { Name = "Nhiệt độ", Type = "TEMPERATURE", Unit = "°C" });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: List ────────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<Config>
            {
                Items      = [SampleConfig],
                Page       = 1,
                PageSize   = 10,
                TotalItems = 1,
                TotalPages = 1
            };
            _configUseCase.Setup(x => x.List(null, null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Lọc theo type HUMIDITY → 200
        public async Task List_FilterByType_Returns200()
        {
            var paged = new PagedResult<Config> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _configUseCase.Setup(x => x.List("HUMIDITY", null, 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("HUMIDITY", null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F03: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy chi tiết config tồn tại → 200
        public async Task GetById_ExistingConfig_Returns200()
        {
            _configUseCase.Setup(x => x.GetById(ConfigId))
                .ReturnsAsync(ControllerTestBase.OkResult<Config?>(SampleConfig));

            var result = await _controller.GetById(ConfigId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Config không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _configUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Config?>("Không tìm thấy config"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F04: Update ──────────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Cập nhật config hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _configUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateConfigCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(ConfigId, new UpdateConfigRequest { Name = "Nhiệt độ (cập nhật)", Type = "TEMPERATURE", Unit = "°C" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Config không tồn tại → 404
        public async Task Update_ConfigNotFound_Returns404()
        {
            _configUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateConfigCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy config"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateConfigRequest { Name = "X", Type = "T", Unit = "u" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F05: Delete ──────────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Xóa config tồn tại → 200
        public async Task Delete_ExistingConfig_Returns200()
        {
            _configUseCase.Setup(x => x.Delete(ConfigId))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Delete(ConfigId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Config không tồn tại → 404
        public async Task Delete_ConfigNotFound_Returns404()
        {
            _configUseCase.Setup(x => x.Delete(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy config"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F05-TC03: Config đang được sử dụng bởi model → 400
        public async Task Delete_ConfigInUse_Returns400()
        {
            _configUseCase.Setup(x => x.Delete(ConfigId))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Config đang được sử dụng"));

            var result = await _controller.Delete(ConfigId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
