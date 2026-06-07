using System.Text.Json;

namespace IncuSmart.Core.Usecases
{
    public class DeviceCommandUseCase : IDeviceCommandUseCase
    {
        private readonly IMqttPublisher               _mqtt;
        private readonly ILogger<DeviceCommandUseCase> _logger;

        public DeviceCommandUseCase(
            IMqttPublisher                mqtt,
            ILogger<DeviceCommandUseCase> logger)
        {
            _mqtt   = mqtt;
            _logger = logger;
        }

        // Topic cố định khớp với ESP32 firmware
        private const string CommandTopic = "egg_incubator/command";

        public async Task<ResultModel<bool>> SetPower(Guid incubatorId, bool on)
        {
            // on=true  → heater_auto, on=false → heater_off
            var cmd     = on ? "heater_auto" : "heater_off";
            var payload = JsonSerializer.Serialize(new { cmd });
            await _mqtt.PublishAsync(CommandTopic, payload);

            _logger.LogInformation("[MQTT] SetPower={On} → cmd={Cmd}", on, cmd);
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> SetHeaterMode(Guid incubatorId, string mode)
        {
            if (mode != "AUTO" && mode != "MANUAL")
                return ResultModelUtils.FillResult<bool>("400", "Chế độ heater không hợp lệ (AUTO/MANUAL)", false);

            // AUTO → heater_auto, MANUAL → heater_off
            var cmd     = mode == "AUTO" ? "heater_auto" : "heater_off";
            var payload = JsonSerializer.Serialize(new { cmd });
            await _mqtt.PublishAsync(CommandTopic, payload);

            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> SetFanMode(Guid incubatorId, string mode)
        {
            if (mode != "AUTO" && mode != "ON" && mode != "OFF")
                return ResultModelUtils.FillResult<bool>("400", "Chế độ fan không hợp lệ (AUTO/ON/OFF)", false);

            // ON=100%, OFF=0%, AUTO=75%
            var speed = mode switch
            {
                "ON"   => 100,
                "OFF"  => 0,
                _      => 75
            };
            var payload = JsonSerializer.Serialize(new { cmd = "set_fan_speed", value = speed });
            await _mqtt.PublishAsync(CommandTopic, payload);

            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }
    }
}
