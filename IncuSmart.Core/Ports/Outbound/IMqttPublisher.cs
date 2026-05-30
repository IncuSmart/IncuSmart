namespace IncuSmart.Core.Ports.Outbound
{
    public interface IMqttPublisher
    {
        Task PublishAsync(string topic, string payload, CancellationToken ct = default);
    }
}
