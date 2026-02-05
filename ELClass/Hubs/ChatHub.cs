using System.Security.Claims;
using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Models;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OnlineUserTracker _tracker;

    public ChatHub(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        OnlineUserTracker tracker
    )
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _tracker = tracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var becameOnline = _tracker.Add(userId, Context.ConnectionId);
            if (becameOnline)
            {
                await Clients.All.SendAsync("PresenceChanged", userId, true);
            }

            Console.WriteLine($"[Presence] {userId} connected");
        }

        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var becameOffline = _tracker.Remove(userId, Context.ConnectionId);
            if (becameOffline)
            {
                await Clients.All.SendAsync("PresenceChanged", userId, false);
            }

            Console.WriteLine($"[Presence] {userId} disconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }


    public async Task SendMessage(string receiverId, string message)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(senderId)) return;

        var time = DateTime.UtcNow;

        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message, time);
        await Clients.Users(receiverId, senderId).SendAsync("UpdateConversationList");
    }


    public async Task MarkAsRead(int conversationId, string otherUserId)
    {
        var me = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(me)) return;
        if (conversationId <= 0) return;
        if (string.IsNullOrWhiteSpace(otherUserId)) return;

        var unread = await _unitOfWork.CHMessageRepository.GetAsync(
            m => m.ConversationId == conversationId
                 && m.SenderId == otherUserId
                 && m.ReceiverId == me
                 && m.IsRead == false,
            tracked: true,
            orderBy: q => q.OrderBy(m => m.Id)
        );

        if (unread == null || !unread.Any()) return;

        var now = DateTime.UtcNow;
        var ids = unread.Select(m => m.Id).ToList();

        foreach (var m in unread)
        {
            m.IsRead = true;
            m.ReadAt = now;
        }

        await _unitOfWork.CommitAsync();

        await Clients.User(otherUserId).SendAsync("MessagesRead", new
        {
            conversationId = conversationId,
            messageIds = ids
        });
    }



    public Task<bool> IsUserOnline(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(false);

        return Task.FromResult(_tracker.IsOnline(userId));
    }
    public Task<string?> WhoAmI()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Task.FromResult(userId);
    }



}


