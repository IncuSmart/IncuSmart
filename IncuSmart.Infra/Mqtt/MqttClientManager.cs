using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

namespace IncuSmart.Infra.Mqtt
{
    // Singleton wrapper so BackgroundService and MqttPublisher share one client instance
    public class MqttClientManager
    {
        public IManagedMqttClient Client { get; }

        public MqttClientManager()
        {
            var factory = new MqttFactory();
            Client = factory.CreateManagedMqttClient();
        }
    }
}
