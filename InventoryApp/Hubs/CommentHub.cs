using Microsoft.AspNetCore.SignalR;

namespace InventoryApp.Hubs
{
    public class CommentHub : Hub
    {
        public async Task JoinInventory(string inventoryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, inventoryId);
        }

        public async Task LeaveInventory(string inventoryId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, inventoryId);
        }
    }
}