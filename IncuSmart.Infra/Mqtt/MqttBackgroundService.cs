using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System.Text.Json;

namespace IncuSmart.Infra.Mqtt
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly MqttClientManager _manager;
        private readonly MqttOptions       _options;
        private readonly IDeviceNotifier   _notifier;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MqttBackgroundService> _logger;

        public MqttBackgroundService(
            MqttClientManager                manager,
            IOptions<MqttOptions>            options,
            IDeviceNotifier                  notifier,
            IServiceScopeFactory             scopeFactory,
            ILogger<MqttBackgroundService>   logger)
        {
            _manager      = manager;
            _options      = options.Value;
            _notifier     = notifier;
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = _manager.Client;

            client.ApplicationMessageReceivedAsync += HandleMessageAsync;
            client.ConnectedAsync    += e => { _logger.LogInformation("[MQTT] Connected to broker"); return Task.CompletedTask; };
            client.DisconnectedAsync += e => { _logger.LogWarning("[MQTT] Disconnected: {Reason}", e.ReasonString); return Task.CompletedTask; };

            var opts = BuildOptions();
            await client.StartAsync(opts);

            // Topics khớp với ESP32 firmware (egg_incubator/...)
            await client.SubscribeAsync(new[]
            {
                new MqttTopicFilterBuilder()
                    .WithTopic("egg_incubator/telemetry")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(),
                new MqttTopicFilterBuilder()
                    .WithTopic("egg_incubator/status")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(),
                new MqttTopicFilterBuilder()
                    .WithTopic("egg_incubator/alarm")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            });

            _logger.LogInformation("[MQTT] Subscribed — waiting for device messages");

            await Task.Delay(Timeout.Infinite, stoppingToken);

            await client.StopAsync();
        }

        private ManagedMqttClientOptions BuildOptions()
        {
            var clientOptsBuilder = new MqttClientOptionsBuilder()
                .WithClientId($"incusmart-be-{Guid.NewGuid():N}")
                .WithTcpServer(_options.Host, _options.Port)
                .WithCleanSession();

            if (!string.IsNullOrEmpty(_options.Username))
                clientOptsBuilder.WithCredentials(_options.Username, _options.Password);

            if (_options.UseTls)
            {
                clientOptsBuilder.WithTlsOptions(o =>
                    o.WithCertificateValidationHandler(_ => true));
            }

            return new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(30))
                .WithClientOptions(clientOptsBuilder.Build())
                .Build();
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic   = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            try
            {
                switch (topic)
                {
                    case "egg_incubator/telemetry":
                        await HandleTelemetryAsync(payload);
                        break;
                    case "egg_incubator/status":
                        await HandleStatusAsync(payload);
                        break;
                    case "egg_incubator/alarm":
                        _logger.LogWarning("[MQTT] Alarm received: {Payload}", payload);
                        await _notifier.NotifyTelemetryAsync(_options.DeviceMac, payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] Error handling message on {Topic}", topic);
            }
        }

        private async Task HandleTelemetryAsync(string payload)
        {
            var mac = _options.DeviceMac;

            // Push real-time tới SignalR
            await _notifier.NotifyTelemetryAsync(mac, payload);

            using var scope = _scopeFactory.CreateScope();
            var masterboardRepo   = scope.ServiceProvider.GetRequiredService<IMasterboardRepository>();
            var sensorRepo        = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var sensorReadingRepo = scope.ServiceProvider.GetRequiredService<ISensorReadingRepository>();

            var board = await masterboardRepo.FindByMacAddress(mac);
            if (board == null)
            {
                _logger.LogWarning("[MQTT] Masterboard not found for MAC: {Mac}", mac);
                return;
            }

            await masterboardRepo.UpdateLastSeenAt(board.Id, DateTime.UtcNow);

            // Lưu sensor readings vào DB
            try
            {
                var doc     = JsonDocument.Parse(payload);
                var sensors = await sensorRepo.FindByMasterboardId(board.Id);
                var now     = DateTime.UtcNow;

                foreach (var sensor in sensors)
                {
                    var code  = sensor.HardwareCode?.ToLower() ?? "";
                    decimal? value = null;

                    if (code.Contains("temp") && doc.RootElement.TryGetProperty("temperature", out var tempEl))
                        value = (decimal)tempEl.GetDouble();
                    else if (code.Contains("humid") && doc.RootElement.TryGetProperty("humidity", out var humidEl))
                        value = (decimal)humidEl.GetDouble();

                    if (value.HasValue)
                        await sensorReadingRepo.AddAsync(new SensorReading
                        {
                            SensorId   = sensor.Id,
                            Value      = value,
                            RecordedAt = now,
                            Status     = BaseStatus.ACTIVE
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MQTT] Failed to save sensor readings for {Mac}", mac);
            }
        }

        private async Task HandleStatusAsync(string payload)
        {
            try
            {
                var doc    = JsonDocument.Parse(payload);
                // ESP32 gửi {"status": "online"} hoặc {"status": "offline"}
                var status = doc.RootElement.GetProperty("status").GetString();
                var online = status == "online";
                await _notifier.NotifyStatusAsync(_options.DeviceMac, online);
            }
            catch
            {
                // malformed status packet — ignore
            }
        }
    }
}
