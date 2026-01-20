using System.Security.Claims;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Models;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    public ChatHub(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        this._unitOfWork = unitOfWork;
        this._userManager = userManager;
    }
    public async Task SendMessage(string receiverId, string message)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(senderId)) return;

        var time = DateTime.UtcNow;

        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message, time);
        await Clients.Users(receiverId, senderId).SendAsync("UpdateConversationList");
    }

    // ✅ NEW: لما الطرف التاني يقرا الرسائل
    public async Task MarkAsRead(int conversationId, string otherUserId)
    {
        var readerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(readerId)) return;

        var now = DateTime.UtcNow;

        // هات رسائل الطرف التاني اللي "وصلت للطالب" ولسه مش مقروءة
        var unread = (await _unitOfWork.CHMessageRepository.GetAsync())
             .Where(m => m.ConversationId == conversationId && m.SenderId == otherUserId && m.ReceiverId == readerId && !m.IsRead).ToList();

        if (unread.Count == 0) return;

        foreach (var m in unread)
        {
            m.IsRead = true;
            m.ReadAt = now;
        }

        await _unitOfWork.CommitAsync();

        var messageIds = unread.Select(m => m.Id).ToList();

        // بلّغ الطرف التاني إن الرسائل دي اتقرت
        await Clients.User(otherUserId).SendAsync("MessagesRead", new
        {
            conversationId,
            messageIds,
            readAt = now
        });

        // اختياري: تحديث الليست عند الطرفين
        await Clients.Users(otherUserId, readerId).SendAsync("UpdateConversationList");
    }

}


