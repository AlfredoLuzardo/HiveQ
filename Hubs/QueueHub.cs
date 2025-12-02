using Microsoft.AspNetCore.SignalR;

namespace HiveQ.Hubs
{
    public class QueueHub : Hub
    {
        // Clients can subscribe to specific queue updates
        public async Task SubscribeToQueue(int queueId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Queue_{queueId}");
        }

        public async Task UnsubscribeFromQueue(int queueId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Queue_{queueId}");
        }

        // Subscribe to all queues for home page
        public async Task SubscribeToAllQueues()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllQueues");
        }

        public async Task UnsubscribeFromAllQueues()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AllQueues");
        }
    }
}
