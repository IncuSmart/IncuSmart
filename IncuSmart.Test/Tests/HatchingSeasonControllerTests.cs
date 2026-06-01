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
    public class HatchingSeasonControllerTests
    {
        private readonly Mock<IHatchingSeasonUseCase> _seasonUseCase = new();
        private readonly HatchingSeasonController     _controller;

        private static readonly Guid SeasonId    = Guid.NewGuid();
        private static readonly Guid IncubatorId = Guid.NewGuid();

        private static readonly HatchingSeasonDetailResponse SampleSeasonDetail = new()
        {
            Season  = new HatchingSeason { Id = SeasonId, EggType = "Gà", Status = IncuSmart.Core.Enums.HatchingSeasonStatus.ACTIVE },
            Batches = []
        };

        public HatchingSeasonControllerTests()
        {
            _controller = new HatchingSeasonController(_seasonUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo mùa ấp mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _seasonUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(SeasonId));

            var result = await _controller.Create(new CreateHatchingSeasonRequest
            {
                IncubatorId = IncubatorId,
                EggType     = "Gà",
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
                TemplateId  = null
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Máy ấp không tồn tại → 404
        public async Task Create_IncubatorNotFound_Returns404()
        {
            _seasonUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy máy ấp"));

            var result = await _controller.Create(new CreateHatchingSeasonRequest
            {
                IncubatorId = Guid.NewGuid(),
                EggType     = "Vịt",
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F01-TC03: Customer không có quyền tạo mùa ấp trên máy của người khác → 403
        public async Task Create_IncubatorForbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _seasonUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<Guid?>("Không có quyền"));

            var result = await _controller.Create(new CreateHatchingSeasonRequest
            {
                IncubatorId = IncubatorId,
                EggType     = "Gà",
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        // ─── F02: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy chi tiết mùa ấp tồn tại → 200
        public async Task GetById_ExistingSeason_Returns200()
        {
            _seasonUseCase.Setup(x => x.GetById(SeasonId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<HatchingSeasonDetailResponse?>(SampleSeasonDetail));

            var result = await _controller.GetById(SeasonId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Mùa ấp không tồn tại → 404
        public async Task GetById_NotFound_Returns404()
        {
            _seasonUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<HatchingSeasonDetailResponse?>("Không tìm thấy mùa ấp"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

            // ─── F03: List ────────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<HatchingSeason> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _seasonUseCase.Setup(x => x.List(null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Lọc theo incubatorId → 200
        public async Task List_FilterByIncubatorId_Returns200()
        {
            var paged = new PagedResult<HatchingSeason> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _seasonUseCase.Setup(x => x.List(IncubatorId, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(IncubatorId, null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F04: Update ──────────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Cập nhật mùa ấp hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _seasonUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(SeasonId, new UpdateHatchingSeasonRequest { Notes = "Cập nhật ghi chú mùa ấp" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Mùa ấp không tồn tại → 404
        public async Task Update_SeasonNotFound_Returns404()
        {
            _seasonUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy mùa ấp"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateHatchingSeasonRequest { Notes = "Ghi chú" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F05: UpdateStatus ────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Chuyển trạng thái hợp lệ → 200
        public async Task UpdateStatus_ValidTransition_Returns200()
        {
            _seasonUseCase.Setup(x => x.UpdateStatus(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonStatusCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateStatus(SeasonId, new UpdateHatchingSeasonStatusRequest { Status = "COMPLETED" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Chuyển trạng thái không hợp lệ → 400
        public async Task UpdateStatus_InvalidTransition_Returns400()
        {
            _seasonUseCase.Setup(x => x.UpdateStatus(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonStatusCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Không thể chuyển trạng thái"));

            var result = await _controller.UpdateStatus(SeasonId, new UpdateHatchingSeasonStatusRequest { Status = "ACTIVE" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
