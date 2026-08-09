using customerseller.Helpers;
using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace customerseller.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public IActionResult Messages()
        {
            var (email, role) = GetCurrentUser();
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Home");

            ViewBag.CurrentEmail = email;
            return View("~/Views/Chat/Messages.cshtml");
        }

       
        [HttpGet]
        public IActionResult GetConversations()
        {
            var (email, role) = GetCurrentUser();
            if (string.IsNullOrEmpty(email)) return Json(new List<object>());

            var conversations = _context.Conversations
                .Where(c => c.User1Email == email || c.User2Email == email)
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            var result = conversations.Select(c =>
            {
                var otherEmail = c.User1Email == email ? c.User2Email : c.User1Email;
                var otherRole = c.User1Email == email ? c.User2Role : c.User1Role;

                var otherName = GetDisplayName(otherEmail, otherRole);

                var lastMsg = _context.Messages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                var unreadCount = _context.Messages
                    .Count(m => m.ConversationId == c.Id && m.SenderEmail != email && !m.IsRead);

                return new
                {
                    conversationId = c.Id,
                    otherEmail,
                    otherRole,
                    otherName,
                    lastMessage = lastMsg?.MessageText ?? "",
                    lastMessageAt = lastMsg?.SentAt,
                    unreadCount
                };
            }).ToList();

            return Json(result);
        }
        [HttpGet]
        public IActionResult GetChatPartners()
        {
            var (email, role) = GetCurrentUser();
            var allowedRoles = ChatPermissions.GetAllowedRolesFor(role);
            var result = new List<object>();

            // Users table se (Customer, Seller)
            var users = _context.Users
                .Where(u => allowedRoles.Contains(u.Role) && u.Email != email)
                .ToList();
            result.AddRange(users.Select(u => new
            {
                email = u.Email,
                role = u.Role,
                name = GetDisplayName(u.Email, u.Role)
            }));

            // Courier
            if (allowedRoles.Contains("Courier"))
            {
                var couriers = _context.CourierCompanies.Where(c => c.Email != email).ToList();
                result.AddRange(couriers.Select(c => new { email = c.Email, role = "Courier", name = c.CompanyName }));
            }

            // Admin (fixed single account)
            if (allowedRoles.Contains("Admin") && email != "artisanvalley.store@gmail.com")
            {
                result.Add(new { email = "artisanvalley.store@gmail.com", role = "Admin", name = "Admin Support" });
            }

            // Moderator
            if (allowedRoles.Contains("Moderator"))
            {
                var mods = _context.ModeratorSettings.Where(m => m.Email != email).ToList();
                result.AddRange(mods.Select(m => new { email = m.Email, role = "Moderator", name = "Moderator (" + m.Email + ")" }));
            }

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetMessages(int conversationId)
        {
            var (email, _) = GetCurrentUser();
            if (string.IsNullOrEmpty(email)) return Json(new List<object>());

            var conversation = _context.Conversations.FirstOrDefault(c => c.Id == conversationId);
            if (conversation == null) return NotFound();

            if (conversation.User1Email != email && conversation.User2Email != email)
                return Forbid();

            var messages = _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToList();

          
            var unread = messages.Where(m => m.SenderEmail != email && !m.IsRead).ToList();
            foreach (var m in unread) m.IsRead = true;
            if (unread.Any()) _context.SaveChanges();

            var result = messages.Select(m => new
            {
                id = m.Id,
                senderEmail = m.SenderEmail,
                senderRole = m.SenderRole,
                messageText = m.MessageText,
                sentAt = m.SentAt,
                isMine = m.SenderEmail == email,
                isEdited = m.IsEdited   // yeh line add ki
            }).ToList();

            return Json(result);
        }

       
        [HttpPost]
        public IActionResult StartConversation(string otherEmail, string otherRole, string productId = null)
        {
            var (email, role) = GetCurrentUser();

            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Login required." });

            if (!ChatPermissions.IsAllowed(role, otherRole))
                return Json(new { success = false, message = "This chat is not allowed." });

            var existing = _context.Conversations.FirstOrDefault(c =>
                ((c.User1Email == email && c.User2Email == otherEmail) ||
                 (c.User1Email == otherEmail && c.User2Email == email)) &&
                c.RelatedProductId == productId);

            if (existing != null)
                return Json(new { success = true, conversationId = existing.Id });

            var conversation = new Conversation
            {
                User1Email = email,
                User1Role = role,
                User2Email = otherEmail,
                User2Role = otherRole,
                RelatedProductId = productId
            };

            _context.Conversations.Add(conversation);
            _context.SaveChanges();

            return Json(new { success = true, conversationId = conversation.Id });
        }

        
        [HttpGet]
        public IActionResult GetUnreadCount()
        {
            var (email, _) = GetCurrentUser();
            if (string.IsNullOrEmpty(email)) return Json(new { count = 0 });

            var myConversationIds = _context.Conversations
                .Where(c => c.User1Email == email || c.User2Email == email)
                .Select(c => c.Id)
                .ToList();

            var count = _context.Messages
                .Count(m => myConversationIds.Contains(m.ConversationId)
                       && m.SenderEmail != email && !m.IsRead);

            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var msg = await _context.Messages.FindAsync(messageId);
            if (msg == null) return Json(new { success = false, message = "Message not found." });

            var (email, _) = GetCurrentUser();
            if (string.IsNullOrEmpty(email) || msg.SenderEmail != email)
                return Json(new { success = false, message = "You can only delete your own messages." });

            _context.Messages.Remove(msg);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int messageId, string newText)
        {
            var msg = await _context.Messages.FindAsync(messageId);
            if (msg == null) return Json(new { success = false, message = "Message not found." });

            var (email, _) = GetCurrentUser();
            if (string.IsNullOrEmpty(email) || msg.SenderEmail != email)
                return Json(new { success = false, message = "You can only edit your own messages." });

            if (string.IsNullOrWhiteSpace(newText))
                return Json(new { success = false, message = "Message cannot be empty." });

            if ((DateTime.Now - msg.SentAt).TotalMinutes > 5)
                return Json(new { success = false, message = ""});

            msg.MessageText = newText;
            msg.IsEdited = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        private string GetDisplayName(string email, string role)
        {
            if (role == "Courier")
            {
                var courier = _context.CourierCompanies.FirstOrDefault(c => c.Email == email);
                if (courier != null) return courier.CompanyName;
            }
            if (role == "Seller")
            {
                var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);
                if (shop != null && !string.IsNullOrEmpty(shop.ShopName))
                    return shop.ShopName;
            }
            if (role == "Admin")
            {
                return "Admin Support";
            }
            if (role == "Moderator")
            {
                return "Moderator";
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
                return $"{user.FirstName} {user.LastName}".Trim();

            return email;
        }
        private (string Email, string Role) GetCurrentUser()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(email))
                return (email, role);

            var courierEmail = HttpContext.Session.GetString("CourierEmail");
            if (!string.IsNullOrEmpty(courierEmail))
                return (courierEmail, "Courier");

            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (!string.IsNullOrEmpty(adminEmail))
                return (adminEmail, "Admin");

            var modEmail = HttpContext.Session.GetString("ModeratorEmail");
            var modRole = HttpContext.Session.GetString("ModeratorRole");
            if (!string.IsNullOrEmpty(modEmail))
                return (modEmail, modRole ?? "Moderator");

            return (null, null);
        }
      
    }
}
