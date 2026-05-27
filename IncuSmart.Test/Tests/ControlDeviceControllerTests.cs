using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Test.Helpers;
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

        public ControlDeviceControllerTests()
        {
            _controller = new ControlDeviceController(_controlDeviceUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task Create_DuplicateDeviceCode_Returns409()
        {
            _controlDeviceUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateControlDeviceCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Mã thiết bị đã tồn tại"));

            var result = await _controller.Create(new CreateControlDeviceRequest
            {
                MasterboardId = Guid.NewGuid(),
                ConfigId      = Guid.NewGuid(),
                HardwareCode  = "EXISTING-DEVICE"
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── GetByIncubatorId ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIncubatorId_ValidIncubator_Returns200()
        {
            var devices = new List<ControlDevice>
            {
                new() { Id = DeviceId, MasterboardId = Guid.NewGuid(), ConfigId = Guid.NewGuid(), HardwareCode = "FAN-001", Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }
            };
            _controlDeviceUseCase.Setup(x => x.GetByIncubatorId(IncubatorId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<ControlDevice>>(devices));

            var result = await _controller.GetByIncubatorId(IncubatorId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetByIncubatorId_IncubatorNotFound_Returns404()
        {
            _controlDeviceUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<ControlDevice>>("Không tìm thấy máy ấp"));

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetByIncubatorId_CustomerForbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _controlDeviceUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(new IncuSmart.Core.ResultModel<List<ControlDevice>> { StatusCode = "403", Message = "Không có quyền xem thiết bị này" });

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }
    }
}
