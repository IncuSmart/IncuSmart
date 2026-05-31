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
    public class HatchingSeasonTemplateControllerTests
    {
        private readonly Mock<IHatchingSeasonTemplateUseCase> _templateUseCase = new();
        private readonly HatchingSeasonTemplateController     _controller;

        private static readonly Guid TemplateId = Guid.NewGuid();

        private static readonly HatchingSeasonTemplateDetailResponse SampleTemplateDetail = new()
        {
            Template = new HatchingSeasonTemplate { Id = TemplateId, Name = "Mẫu Ấp Gà 21 Ngày", EggType = "Gà", Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE },
            Batches  = []
        };

        public HatchingSeasonTemplateControllerTests()
        {
            _controller = new HatchingSeasonTemplateController(_templateUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo template mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _templateUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonTemplateCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(TemplateId));

            var result = await _controller.Create(new CreateHatchingSeasonTemplateRequest
            {
                Name    = "Mẫu Ấp Gà 21 Ngày",
                EggType = "Gà",
                Batches = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Tên template đã tồn tại → 409
        public async Task Create_DuplicateName_Returns409()
        {
            _templateUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonTemplateCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Tên template đã tồn tại"));

            var result = await _controller.Create(new CreateHatchingSeasonTemplateRequest
            {
                Name    = "Mẫu Ấp Gà 21 Ngày",
                EggType = "Gà",
                Batches = []
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy chi tiết template tồn tại → 200
        public async Task GetById_ExistingTemplate_Returns200()
        {
            _templateUseCase.Setup(x => x.GetById(TemplateId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<HatchingSeasonTemplateDetailResponse?>(SampleTemplateDetail));

            var result = await _controller.GetById(TemplateId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Template không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _templateUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<HatchingSeasonTemplateDetailResponse?>("Không tìm thấy template"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F03: List ────────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<HatchingSeasonTemplate> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _templateUseCase.Setup(x => x.List(null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Lọc theo loại người tạo TECHNICIAN → 200
        public async Task List_FilterByCreatedByType_Returns200()
        {
            var paged = new PagedResult<HatchingSeasonTemplate> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _templateUseCase.Setup(x => x.List(null, "TECHNICIAN", It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, "TECHNICIAN", new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F04: Update ──────────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Cập nhật template hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _templateUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonTemplateCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(TemplateId, new UpdateHatchingSeasonTemplateRequest { Name = "Mẫu Ấp Gà 21 Ngày v2", EggType = "Gà", Batches = [] });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Template không tồn tại → 404
        public async Task Update_TemplateNotFound_Returns404()
        {
            _templateUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonTemplateCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy template"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateHatchingSeasonTemplateRequest { Name = "X", EggType = "X", Batches = [] });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F04-TC03: Customer không có quyền chỉnh sửa template của người khác → 403
        public async Task Update_Forbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _templateUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonTemplateCommand>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<bool>("Không có quyền chỉnh sửa template này"));

            var result = await _controller.Update(TemplateId, new UpdateHatchingSeasonTemplateRequest { Name = "X", EggType = "X", Batches = [] });

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        // ─── F05: Delete ──────────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Xóa template của mình → 200
        public async Task Delete_OwnTemplate_Returns200()
        {
            _templateUseCase.Setup(x => x.Delete(TemplateId, It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Delete(TemplateId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Template không tồn tại → 404
        public async Task Delete_TemplateNotFound_Returns404()
        {
            _templateUseCase.Setup(x => x.Delete(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy template"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
