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

            var speed = mode switch { "ON" => 100, "OFF" => 0, _ => 75 };
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "set_fan_speed", value = speed }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> SetFanSpeed(Guid incubatorId, int speed)
        {
            if (speed < 0 || speed > 100)
                return ResultModelUtils.FillResult<bool>("400", "Tốc độ fan phải từ 0-100", false);

            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "set_fan_speed", value = speed }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> SetTemperature(Guid incubatorId, double value)
        {
            if (value < 30.0 || value > 40.0)
                return ResultModelUtils.FillResult<bool>("400", "Nhiệt độ phải từ 30-40°C", false);

            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "set_temp", value }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> TurnTray(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "turn_tray" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> StopAutoTurn(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "stop_auto_turn" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> StartAutoTurn(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "start_auto_turn" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> StartIncubation(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "start_incubation" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> ResetIncubation(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "reset_incubation" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> DisableFanCheck(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "disable_fan_check" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> EnableFanCheck(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "enable_fan_check" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> ResetWifi(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "reset_wifi" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }

        public async Task<ResultModel<bool>> Reboot(Guid incubatorId)
        {
            await _mqtt.PublishAsync(CommandTopic, JsonSerializer.Serialize(new { cmd = "reboot" }));
            return ResultModelUtils.FillResult<bool>("200", CommonConst.Success, true);
        }
    }
}
