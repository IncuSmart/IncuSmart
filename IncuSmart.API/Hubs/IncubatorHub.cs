using Microsoft.AspNetCore.SignalR;

namespace IncuSmart.API.Hubs
{
    public class IncubatorHub : Hub
    {
        // UI joins a group by MAC address to receive updates for that specific device
        public async Task JoinDevice(string macAddress)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, macAddress);
        }

        public async Task LeaveDevice(string macAddress)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, macAddress);
        }
    }
}
