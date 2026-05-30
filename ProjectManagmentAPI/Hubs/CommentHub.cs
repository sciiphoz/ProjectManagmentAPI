using Microsoft.AspNetCore.SignalR;

namespace ProjectManagementAPI.Hubs
{
    public class CommentHub : Hub
    {
        public async Task SubscribeToTask(string taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, taskId);
        }

        public async Task UnsubscribeFromTask(string taskId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, taskId);
        }
    }
}