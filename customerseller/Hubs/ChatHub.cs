using customerseller.Helpers;
using customerseller.Models;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace customerseller.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var email = GetSessionEmail();
            if (!string.IsNullOrEmpty(email))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, email);
            }
            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "conv_" + conversationId);
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "conv_" + conversationId);
        }

        public async Task SendMessage(int conversationId, string messageText)
        {
            var senderEmail = GetSessionEmail();
            var senderRole = GetSessionRole();

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrWhiteSpace(messageText))
                return;

            var conversation = _context.Conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conversation == null)
                return;

            if (conversation.User1Email != senderEmail && conversation.User2Email != senderEmail)
                return;

            var message = new Message
            {
                ConversationId = conversationId,
                SenderEmail = senderEmail,
                SenderRole = senderRole ?? "",
                MessageText = messageText.Trim(),
                IsRead = false
            };

            _context.Messages.Add(message);
            conversation.LastMessageAt = System.DateTime.Now;
            _context.SaveChanges();

            await Clients.Group("conv_" + conversationId).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderEmail = message.SenderEmail,
                senderRole = message.SenderRole,
                messageText = message.MessageText,
                sentAt = message.SentAt
            });

            var receiverEmail = conversation.User1Email == senderEmail
                ? conversation.User2Email
                : conversation.User1Email;

            await Clients.Group(receiverEmail).SendAsync("NewMessageNotification", new
            {
                conversationId = conversationId
            });
        }

        private string GetSessionEmail()
        {
            var session = Context.GetHttpContext()?.Session;
            if (session == null) return null;

            var email = session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(email)) return email;

            email = session.GetString("CourierEmail");
            if (!string.IsNullOrEmpty(email)) return email;

            email = session.GetString("AdminEmail");
            if (!string.IsNullOrEmpty(email)) return email;

            email = session.GetString("ModeratorEmail");
            return email;
        }

        private string GetSessionRole()
        {
            var session = Context.GetHttpContext()?.Session;
            if (session == null) return null;

            var email = session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(email)) return session.GetString("UserRole");

            if (!string.IsNullOrEmpty(session.GetString("CourierEmail"))) return "Courier";
            if (!string.IsNullOrEmpty(session.GetString("AdminEmail"))) return "Admin";
            if (!string.IsNullOrEmpty(session.GetString("ModeratorEmail")))
                return session.GetString("ModeratorRole") ?? "Moderator";

            return null;
        }
    }
}