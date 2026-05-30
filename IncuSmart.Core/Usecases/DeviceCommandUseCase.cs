using System.Text.Json;

namespace IncuSmart.Core.Usecases
{
    public class DeviceCommandUseCase : IDeviceCommandUseCase
    {
        private readonly IMasterboardRepository _masterboardRepo;
        private readonly IMqttPublisher         _mqtt;
        private readonly ILogger<DeviceCommandUseCase> _logger;

        public DeviceCommandUseCase(
            IMasterboardRepository masterboardRepo,
            IMqttPublisher         mqtt,
            ILogger<DeviceCommandUseCase> logger)
        {
            _masterboardRepo = masterboardRepo;
            _mqtt            = mqtt;
            _logger          = logger;
        }

        public async Task<ResultModel<bool>> SetPower(Guid incubatorId, bool on)
        {
            var mac = await GetMacAddress(incubatorId);
            if (mac == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.MasterboardNotFound, false);

            var payload = JsonSerializer.Serialize(new { cmd = "SET_POWER", value = on });
            await _mqtt.PublishAsync($"incubator/{mac}/command", payload);

            _logger.LogInformation("[MQTT] SET_POWER={On} → {Mac}", on, mac);
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> SetHeaterMode(Guid incubatorId, string mode)
        {
            if (mode != "AUTO" && mode != "MANUAL")
                return ResultModelUtils.FillResult<bool>("400", "Chế độ heater không hợp lệ (AUTO/MANUAL)", false);

            var mac = await GetMacAddress(incubatorId);
            if (mac == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.MasterboardNotFound, false);

            var payload = JsonSerializer.Serialize(new { cmd = "SET_HEATER_MODE", mode });
            await _mqtt.PublishAsync($"incubator/{mac}/command", payload);

            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> SetFanMode(Guid incubatorId, string mode)
        {
            if (mode != "AUTO" && mode != "ON" && mode != "OFF")
                return ResultModelUtils.FillResult<bool>("400", "Chế độ fan không hợp lệ (AUTO/ON/OFF)", false);

            var mac = await GetMacAddress(incubatorId);
            if (mac == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.MasterboardNotFound, false);

            var payload = JsonSerializer.Serialize(new { cmd = "SET_FAN_MODE", mode });
            await _mqtt.PublishAsync($"incubator/{mac}/command", payload);

            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        private async Task<string?> GetMacAddress(Guid incubatorId)
        {
            var board = await _masterboardRepo.FindByIncubatorId(incubatorId);
            return board?.MacAddress?.Replace(":", "").ToLower();
        }
    }
}
