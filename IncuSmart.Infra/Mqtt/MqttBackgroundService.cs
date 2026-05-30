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

            await client.SubscribeAsync(new[]
            {
                new MqttTopicFilterBuilder()
                    .WithTopic("incubator/+/telemetry")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(),
                new MqttTopicFilterBuilder()
                    .WithTopic("incubator/+/status")
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
                .WithCredentials(_options.Username, _options.Password)
                .WithCleanSession();

            if (_options.UseTls)
            {
                clientOptsBuilder.WithTlsOptions(o =>
                    o.WithCertificateValidationHandler(_ => true)); // swap with CA cert in production
            }

            return new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(clientOptsBuilder.Build())
                .Build();
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic    = e.ApplicationMessage.Topic;
            var payload  = e.ApplicationMessage.ConvertPayloadToString();
            var segments = topic.Split('/');

            if (segments.Length < 3) return;

            var mac  = segments[1];
            var type = segments[2];

            try
            {
                switch (type)
                {
                    case "telemetry":
                        await HandleTelemetryAsync(mac, payload);
                        break;
                    case "status":
                        await HandleStatusAsync(mac, payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] Error handling message from {Mac}", mac);
            }
        }

        private async Task HandleTelemetryAsync(string mac, string payload)
        {
            // Push real-time to SignalR (UI subscribes to device group by MAC)
            await _notifier.NotifyTelemetryAsync(mac, payload);

            // Update Masterboard.LastSeenAt
            using var scope = _scopeFactory.CreateScope();
            var masterboardRepo = scope.ServiceProvider.GetRequiredService<IMasterboardRepository>();

            var board = await masterboardRepo.FindByMacAddress(mac);
            if (board != null)
                await masterboardRepo.UpdateLastSeenAt(board.Id, DateTime.UtcNow);
        }

        private async Task HandleStatusAsync(string mac, string payload)
        {
            try
            {
                var doc    = JsonDocument.Parse(payload);
                var online = doc.RootElement.GetProperty("online").GetBoolean();
                await _notifier.NotifyStatusAsync(mac, online);
            }
            catch
            {
                // malformed status packet — ignore
            }
        }
    }
}
