using MQTTnet;
using MQTTnet.Protocol;

namespace IncuSmart.Infra.Mqtt
{
    public class MqttPublisher : IMqttPublisher
    {
        private readonly MqttClientManager _manager;

        public MqttPublisher(MqttClientManager manager)
        {
            _manager = manager;
        }

        public async Task PublishAsync(string topic, string payload, CancellationToken ct = default)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _manager.Client.EnqueueAsync(message);
        }
    }
}
