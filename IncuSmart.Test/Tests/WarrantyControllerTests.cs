using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class WarrantyControllerTests
    {
        private readonly Mock<IWarrantyUseCase> _warrantyUseCase = new();
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly WarrantyController     _controller;

        private static readonly Guid WarrantyId  = Guid.NewGuid();
        private static readonly Guid IncubatorId = Guid.NewGuid();

        private static readonly Warranty SampleWarranty = new()
        {
            Id          = WarrantyId,
            IncubatorId = IncubatorId,
            StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate     = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            Notes       = "Bảo hành 12 tháng",
            Status      = IncuSmart.Core.Enums.BaseStatus.ACTIVE
        };

        public WarrantyControllerTests()
        {
            _controller = new WarrantyController(_warrantyUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo bảo hành mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _warrantyUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateWarrantyCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(WarrantyId));

            var result = await _controller.Create(new CreateWarrantyRequest
            {
                IncubatorId = IncubatorId,
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate     = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                Notes       = "Bảo hành 12 tháng"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Máy ấp không tồn tại → 404
        public async Task Create_IncubatorNotFound_Returns404()
        {
            _warrantyUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateWarrantyCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy máy ấp"));

            var result = await _controller.Create(new CreateWarrantyRequest
            {
                IncubatorId = Guid.NewGuid(),
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate     = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F01-TC03: Máy ấp đã có bảo hành → 409
        public async Task Create_WarrantyAlreadyExists_Returns409()
        {
            _warrantyUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateWarrantyCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Máy ấp đã có bảo hành"));

            var result = await _controller.Create(new CreateWarrantyRequest
            {
                IncubatorId = IncubatorId,
                StartDate   = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate     = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: Update ──────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Gia hạn bảo hành hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _warrantyUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateWarrantyCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(WarrantyId, new UpdateWarrantyRequest
            {
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate   = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                Notes     = "Gia hạn bảo hành thêm 1 năm"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Bảo hành không tồn tại → 404
        public async Task Update_WarrantyNotFound_Returns404()
        {
            _warrantyUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateWarrantyCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy bảo hành"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateWarrantyRequest
            {
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate   = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F03: GetByIncubatorId ────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy bảo hành của máy ấp tồn tại → 200
        public async Task GetByIncubatorId_ExistingWarranty_Returns200()
        {
            _warrantyUseCase.Setup(x => x.GetByIncubatorId(IncubatorId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<Warranty?>(SampleWarranty));

            var result = await _controller.GetByIncubatorId(IncubatorId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F03-TC02: Máy ấp chưa có bảo hành → 404
        public async Task GetByIncubatorId_NotFound_Returns404()
        {
            _warrantyUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Warranty?>("Máy ấp chưa có bảo hành"));

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F03-TC03: Customer xem bảo hành máy của người khác → 403
        public async Task GetByIncubatorId_CustomerOtherMachine_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _warrantyUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<Warranty?>("Không có quyền xem bảo hành này"));

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }
    }
}
