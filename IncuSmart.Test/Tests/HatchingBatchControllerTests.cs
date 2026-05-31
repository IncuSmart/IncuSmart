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

        private static readonly Guid BatchId  = Guid.NewGuid();
        private static readonly Guid SeasonId = Guid.NewGuid();

        private static readonly HatchingBatchDetailResponse SampleBatchDetail = new()
        {
            Batch   = new HatchingBatch { Id = BatchId, SeasonId = SeasonId, Name = "Giai đoạn 1", BatchIndex = 1, DayStart = 1, DayEnd = 7, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE },
            Configs = []
        };

        public HatchingBatchControllerTests()
        {
            _controller = new HatchingBatchController(_batchUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo giai đoạn ấp mới hợp lệ → 200
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

        [Fact] // F01-TC02: Mùa ấp không tồn tại → 404
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

        [Fact] // F01-TC03: Khoảng ngày bị trùng với giai đoạn khác → 409
        public async Task Create_DuplicateDayRange_Returns409()
        {
            _batchUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Giai đoạn ấp bị trùng ngày"));

            var result = await _controller.Create(new CreateHatchingBatchRequest
            {
                SeasonId   = SeasonId,
                Name       = "Giai đoạn trùng",
                BatchIndex = 2,
                DayStart   = 1,
                DayEnd     = 7,
                Configs    = []
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: GetBySeasonId ───────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy danh sách giai đoạn của mùa ấp tồn tại → 200
        public async Task GetBySeasonId_ExistingSeason_Returns200()
        {
            _batchUseCase.Setup(x => x.GetBySeasonId(SeasonId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<HatchingBatchDetailResponse>>([SampleBatchDetail]));

            var result = await _controller.GetBySeasonId(SeasonId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Mùa ấp không tồn tại → 404
        public async Task GetBySeasonId_SeasonNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.GetBySeasonId(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<HatchingBatchDetailResponse>>("Không tìm thấy mùa ấp"));

            var result = await _controller.GetBySeasonId(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F03: Update ──────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Cập nhật giai đoạn ấp hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _batchUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(BatchId, new UpdateHatchingBatchRequest
            {
                Name     = "Giai đoạn 1 (cập nhật)",
                DayStart = 1,
                DayEnd   = 8,
                Configs  = []
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Giai đoạn ấp không tồn tại → 404
        public async Task Update_BatchNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateHatchingBatchCommand>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy giai đoạn ấp"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateHatchingBatchRequest { Name = "X", DayStart = 1, DayEnd = 2, Configs = [] });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F04: Delete ──────────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Xóa giai đoạn ấp tồn tại → 200
        public async Task Delete_ExistingBatch_Returns200()
        {
            _batchUseCase.Setup(x => x.Delete(BatchId, It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Delete(BatchId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Giai đoạn ấp không tồn tại → 404
        public async Task Delete_BatchNotFound_Returns404()
        {
            _batchUseCase.Setup(x => x.Delete(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy giai đoạn ấp"));

            var result = await _controller.Delete(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
