using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[Authorize]
public class ChatHub : Hub
{
    public async Task SendMessage(string receiverId, string message)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(senderId)) return;

        var time = DateTime.UtcNow;

        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message, time);
        await Clients.Users(receiverId, senderId).SendAsync("UpdateConversationList");

    }
}

