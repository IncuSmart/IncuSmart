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
    public class SensorControllerTests
    {
        private readonly Mock<ISensorUseCase>        _sensorUseCase        = new();
        private readonly Mock<ISensorReadingUseCase> _sensorReadingUseCase = new();
        private readonly SensorController            _controller;

        private static readonly Guid IncubatorId = Guid.NewGuid();
        private static readonly Guid SensorId    = Guid.NewGuid();

        public SensorControllerTests()
        {
            _controller = new SensorController(_sensorUseCase.Object, _sensorReadingUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidRequest_Returns200()
        {
            _sensorUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateSensorCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(SensorId));

            var result = await _controller.Create(new CreateSensorRequest
            {
                MasterboardId    = Guid.NewGuid(),
                ConfigInstanceId = Guid.NewGuid(),
                HardwareCode     = "TEMP-001",
                PinNumber        = 1
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_IncubatorNotFound_Returns404()
        {
            _sensorUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateSensorCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<Guid?>("Không tìm thấy máy ấp"));

            var result = await _controller.Create(new CreateSensorRequest
            {
                MasterboardId    = Guid.NewGuid(),
                ConfigInstanceId = Guid.NewGuid(),
                HardwareCode     = "TEMP-002"
            });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_DuplicateSensorCode_Returns409()
        {
            _sensorUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateSensorCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Mã sensor đã tồn tại"));

            var result = await _controller.Create(new CreateSensorRequest
            {
                MasterboardId    = Guid.NewGuid(),
                ConfigInstanceId = Guid.NewGuid(),
                HardwareCode     = "EXISTING-CODE"
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── GetByIncubatorId ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIncubatorId_ValidIncubator_Returns200()
        {
            var sensors = new List<Sensor>
            {
                new() { Id = SensorId, MasterboardId = Guid.NewGuid(), ConfigInstanceId = Guid.NewGuid(), HardwareCode = "TEMP-001", Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }
            };
            _sensorUseCase.Setup(x => x.GetByIncubatorId(IncubatorId, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<Sensor>>(sensors));

            var result = await _controller.GetByIncubatorId(IncubatorId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetByIncubatorId_IncubatorNotFound_Returns404()
        {
            _sensorUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<Sensor>>("Không tìm thấy máy ấp"));

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetByIncubatorId_CustomerForbidden_Returns403()
        {
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.CustomerId, "CUSTOMER");
            _sensorUseCase.Setup(x => x.GetByIncubatorId(It.IsAny<Guid>(), It.IsAny<Guid?>(), "CUSTOMER"))
                .ReturnsAsync(new IncuSmart.Core.ResultModel<List<Sensor>> { StatusCode = "403", Message = "Không có quyền xem sensor này" });

            var result = await _controller.GetByIncubatorId(Guid.NewGuid());

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        // ─── GetSensorReadings ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetSensorReadings_NoFilter_Returns200()
        {
            _sensorReadingUseCase.Setup(x => x.GetByFilters(IncubatorId, null, null, null, null, 100, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<SensorReading>>([]));

            var result = await _controller.GetSensorReadings(IncubatorId, null, null, null, null, 100);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSensorReadings_FilterBySensorId_Returns200()
        {
            _sensorReadingUseCase.Setup(x => x.GetByFilters(IncubatorId, SensorId, null, null, null, 50, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<SensorReading>>([]));

            var result = await _controller.GetSensorReadings(IncubatorId, SensorId, null, null, null, 50);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSensorReadings_FilterByDateRange_Returns200()
        {
            var from = DateTime.UtcNow.AddHours(-24);
            var to   = DateTime.UtcNow;
            _sensorReadingUseCase.Setup(x => x.GetByFilters(IncubatorId, null, null, from, to, 100, It.IsAny<Guid?>(), "ADMIN"))
                .ReturnsAsync(ControllerTestBase.OkResult<List<SensorReading>>([]));

            var result = await _controller.GetSensorReadings(IncubatorId, null, null, from, to, 100);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetSensorReadings_IncubatorNotFound_Returns404()
        {
            _sensorReadingUseCase.Setup(x => x.GetByFilters(It.IsAny<Guid>(), null, null, null, null, 100, It.IsAny<Guid?>(), It.IsAny<string>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<List<SensorReading>>("Không tìm thấy máy ấp"));

            var result = await _controller.GetSensorReadings(Guid.NewGuid(), null, null, null, null, 100);

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
