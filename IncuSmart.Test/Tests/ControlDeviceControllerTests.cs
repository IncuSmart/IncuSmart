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
    public class ControlDeviceControllerTests
    {
        private readonly Mock<IControlDeviceUseCase> _controlDeviceUseCase = new();
        private readonly ControlDeviceController     _controller;

        private static readonly Guid IncubatorId = Guid.NewGuid();
        private static readonly Guid DeviceId    = Guid.NewGuid();

        private static readonly List<ControlDevice> SampleDevices =
        [
            new() { Id = DeviceId, MasterboardId = Guid.NewGuid(), ConfigId = Guid.NewGuid(), HardwareCode = "FAN-001", Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }
        ];

        public ControlDeviceControllerTests()
        {
            _controller = new ControlDeviceController(_controlDeviceUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo thiết bị điều khiển mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _controlDeviceUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateControlDeviceCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(DeviceId));

            var result = await _controller.Create(new CreateControlDeviceRequest
            {
                MasterboardId = Guid.NewGuid(),
                ConfigId      = Guid.NewGuid(),
                HardwareCode  = "FAN-001",
                PinNumber     = 2,
                State         = "OFF"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Masterboard không tồn tại → 404
        public async Task Create_IncubatorNotFound_Returns404()
        {
            _controlDeviceUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateControlDeviceCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy máy ấp"));

            var result = await _controller.Create(new CreateControlDeviceRequest
            {
                MasterboardId = Guid.NewGuid(),
                ConfigId      = Guid.NewGuid(),
                HardwareCode  = "FAN-002"
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F01-TC03: Mã hardware thiết bị đã tồn tại → 409
        public async Task Create_DuplicateDeviceCode_Returns409()
        {
            _controlDeviceUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateControlDeviceCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Mã thiết bị đã tồn tại"));

            var result = await _controller.Create(new CreateControlDeviceRequest
            {
                MasterboardId = Guid.NewGuid(),
                ConfigId      = Guid.NewGuid(),
                HardwareCode  = "FAN-001"
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: GetByIncubatorId ────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy danh sách thiết bị của máy ấp hợp lệ → 200
        public async Task GetByIncubatorId_ValidIncubator_Returns200()
        {
            _controlDeviceUseCase.Setup(x => x.GetByIncubatorId(IncubatorId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<ControlDevice>>(SampleDevices));

            var result = await _controller.GetByIncubatorId(IncubatorId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Máy ấp không tồn tại → 404
        public async Task GetByIncubatorId_IncubatorNotFound_Returns404()
        {
            _controlDeviceUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<ControlDevice>>("Không tìm thấy máy ấp"));

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact] // F02-TC03: Customer xem thiết bị máy của người khác → 403
        public async Task GetByIncubatorId_CustomerForbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _controlDeviceUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(ControllerTestBase.ForbiddenResult<List<ControlDevice>>("Không có quyền xem thiết bị này"));

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }
    }
}
