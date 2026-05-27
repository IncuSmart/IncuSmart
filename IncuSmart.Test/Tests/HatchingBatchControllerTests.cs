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
    public class HatchingBatchControllerTests
    {
        private readonly Mock<IHatchingBatchUseCase> _batchUseCase = new();
        private readonly HatchingBatchController     _controller;

        private static readonly Guid BatchId    = Guid.NewGuid();
        private static readonly Guid SeasonId   = Guid.NewGuid();

        public HatchingBatchControllerTests()
        {
            _controller = new HatchingBatchController(_batchUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidRequest_Returns200()
        {
            _batchUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(BatchId));

            var result = await _controller.Create(new CreateHatchingBatchRequest
            {
                SeasonId   = SeasonId,
                Name       = "Giai đoạn 1",
                BatchIndex = 1,
                DayStart   = 1,
                DayEnd     = 7,
                Configs    = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_SeasonNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy mùa ấp"));

            var result = await _controller.Create(new CreateHatchingBatchRequest
            {
                SeasonId   = Guid.NewGuid(),
                Name       = "Giai đoạn X",
                BatchIndex = 1,
                DayStart   = 1,
                DayEnd     = 7,
                Configs    = []
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_DuplicateDayRange_Returns409()
        {
            _batchUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Giai đoạn ấp bị trùng ngày"));

            var result = await _controller.Create(new CreateHatchingBatchRequest
            {
                SeasonId   = SeasonId,
                Name       = "Giai đoạn trùng",
                BatchIndex = 1,
                DayStart   = 1,
                DayEnd     = 7,
                Configs    = []
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── GetBySeasonId ────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetBySeasonId_ExistingSeason_Returns200()
        {
            var batches = new List<HatchingBatchDetailResponse>
            {
                new() { Batch = new HatchingBatch { Id = BatchId, SeasonId = SeasonId, Name = "Giai đoạn 1", BatchIndex = 1, DayStart = 1, DayEnd = 7, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }, Configs = [] }
            };
            _batchUseCase.Setup(x => x.GetBySeasonId(SeasonId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<HatchingBatchDetailResponse>>(batches));

            var result = await _controller.GetBySeasonId(SeasonId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetBySeasonId_SeasonNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.GetBySeasonId(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<HatchingBatchDetailResponse>>("Không tìm thấy mùa ấp"));

            var result = await _controller.GetBySeasonId(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Update ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            _batchUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(BatchId, new UpdateHatchingBatchRequest
            {
                Name     = "Giai đoạn 1 (updated)",
                DayStart = 1,
                DayEnd   = 8,
                Configs  = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_BatchNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy giai đoạn ấp"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateHatchingBatchRequest { Name = "X", DayStart = 1, DayEnd = 2, Configs = [] });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Delete ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ExistingBatch_Returns200()
        {
            _batchUseCase.Setup(x => x.Delete(BatchId, It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Delete(BatchId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Delete_BatchNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.Delete(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy giai đoạn ấp"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
