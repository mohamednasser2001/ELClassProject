using Microsoft.AspNetCore.SignalR;

namespace ELClass.Hubs
{
    public class ChatHub:Hub
    {
        public async Task SendMessage(string receiverId, string message)
        {
            // Context.UserIdentifier هو الـ UserId بتاع الشخص اللي باعت حالياً
            var senderId = Context.UserIdentifier;
            var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            // إرسال الرسالة للمستلم فقط (Receiver)
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message, timestamp);
        }
    }
}
