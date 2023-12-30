using Microsoft.AspNetCore.SignalR;

namespace BusinessLogic.Utils.Notification.Implements
{
    public class ScheduleRouteHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            await base.OnDisconnectedAsync(ex);
        }
    }
}
