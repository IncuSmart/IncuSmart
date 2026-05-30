namespace IncuSmart.Core.Ports.Outbound
{
    public interface IDeviceNotifier
    {
        Task NotifyTelemetryAsync(string macAddress, string payload);
        Task NotifyStatusAsync(string macAddress, bool online);
    }
}
