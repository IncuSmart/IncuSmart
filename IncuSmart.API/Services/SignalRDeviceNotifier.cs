using IncuSmart.API.Hubs;
using IncuSmart.Core.Ports.Outbound;
using Microsoft.AspNetCore.SignalR;

namespace IncuSmart.API.Services
{
    public class SignalRDeviceNotifier : IDeviceNotifier
    {
        private readonly IHubContext<IncubatorHub> _hub;

        public SignalRDeviceNotifier(IHubContext<IncubatorHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyTelemetryAsync(string macAddress, string payload)
            => _hub.Clients.Group(macAddress).SendAsync("Telemetry", payload);

        public Task NotifyStatusAsync(string macAddress, bool online)
            => _hub.Clients.Group(macAddress).SendAsync("Status", online);
    }
}
