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
    public class HatchingSeasonControllerTests
    {
        private readonly Mock<IHatchingSeasonUseCase> _seasonUseCase = new();
        private readonly HatchingSeasonController     _controller;

        private static readonly Guid SeasonId = Guid.NewGuid();

        public HatchingSeasonControllerTests()
        {
            _controller = new HatchingSeasonController(_seasonUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidRequest_Returns200()
        {
            _seasonUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(SeasonId));

            var result = await _controller.Create(new CreateHatchingSeasonRequest
            {
                IncubatorId = Guid.NewGuid(),
                EggType     = "Vịt",
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
                TemplateId  = null
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_IncubatorNotFound_Returns404()
        {
            _seasonUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy máy ấp"));

            var result = await _controller.Create(new CreateHatchingSeasonRequest
            {
                IncubatorId = Guid.NewGuid(),
                EggType     = "Gà",
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_IncubatorForbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _seasonUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(new IncuSmart.Core.ResultModel<Guid?> { StatusCode = "403", Message = "Không có quyền" });

            var result = await _controller.Create(new CreateHatchingSeasonRequest
            {
                IncubatorId = Guid.NewGuid(),
                EggType     = "Gà",
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingSeason_Returns200()
        {
            var season = new HatchingSeasonDetailResponse { Season = new HatchingSeason { Id = SeasonId, EggType = "Gà", Status = IncuSmart.Core.Enums.HatchingSeasonStatus.ACTIVE }, Batches = [] };
            _seasonUseCase.Setup(x => x.GetById(SeasonId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<HatchingSeasonDetailResponse?>(season));

            var result = await _controller.GetById(SeasonId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _seasonUseCase.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<HatchingSeasonDetailResponse?>("Không tìm thấy mùa ấp"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200()
        {
            var paged = new PagedResult<HatchingSeason> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _seasonUseCase.Setup(x => x.List(null, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task List_FilterByIncubatorId_Returns200()
        {
            var incubatorId = Guid.NewGuid();
            var paged       = new PagedResult<HatchingSeason> { Items = [], Page = 1, PageSize = 10, TotalItems = 0, TotalPages = 0 };
            _seasonUseCase.Setup(x => x.List(incubatorId, null, null, It.IsAny<Guid?>(), "ADMIN", 1, 10))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(incubatorId, null, null, new PagingRequest { Page = 1, PageSize = 10 });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── Update ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            _seasonUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(SeasonId, new UpdateHatchingSeasonRequest { Notes = "Cập nhật mùa ấp" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_SeasonNotFound_Returns404()
        {
            _seasonUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy mùa ấp"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateHatchingSeasonRequest { Notes = "Ghi chú" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── UpdateStatus ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatus_ValidTransition_Returns200()
        {
            _seasonUseCase.Setup(x => x.UpdateStatus(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonStatusCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateStatus(SeasonId, new UpdateHatchingSeasonStatusRequest { Status = "COMPLETED" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateStatus_InvalidTransition_Returns400()
        {
            _seasonUseCase.Setup(x => x.UpdateStatus(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingSeasonStatusCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Không thể chuyển trạng thái"));

            var result = await _controller.UpdateStatus(SeasonId, new UpdateHatchingSeasonStatusRequest { Status = "ACTIVE" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
