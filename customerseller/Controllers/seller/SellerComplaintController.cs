using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net;

namespace customerseller.Controllers
{
    public class SellerComplaintController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SellerComplaintController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");
            if (HttpContext.Session.GetString("UserRole") != "Seller")
                return RedirectToAction("Index", "Home");

            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);
            string sellerName = shop?.ShopName ?? "";

            var complaints = _context.Complaints
                               .Where(c => c.SellerName.ToLower() == sellerName.ToLower()
                         && c.Status != "Open" && c.Status != "Fake")
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
          
            if (shop != null)
            {
                shop.ComplaintsLastSeenAt = DateTime.Now;
                _context.SaveChanges();
            }
            return View(complaints);
        }

        public IActionResult GetCount()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { count = 0 });

            if (HttpContext.Session.GetString("UserRole") != "Seller")
                return Json(new { count = 0 });

            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);
            string sellerName = shop?.ShopName ?? "";
            var lastSeen = shop?.ComplaintsLastSeenAt ?? DateTime.MinValue;

            var count = _context.Complaints
                .Count(c => c.SellerName.ToLower() == sellerName.ToLower()
                       && c.Status == "Forwarded"
                       && (c.UpdatedAt ?? c.CreatedAt) > lastSeen);

            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> Respond(int complaintId, string response, IFormFile? proofImage)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });
            response = (response ?? "").Trim();
            if (response.Length < 10 || response.Length > 1000)
                return Json(new { error = "Response must be between 10 and 1000 characters." });

            if (complaint.Status != "Forwarded")
                return Json(new { error = "This complaint is not awaiting your response." });

            var myShop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail.ToLower() == email.ToLower());
            if (myShop == null || !string.Equals(myShop.ShopName, complaint.SellerName, StringComparison.OrdinalIgnoreCase))
                return Json(new { error = "You are not authorized to respond to this complaint." });

            string? proofUrl = null;
            if (proofImage != null && proofImage.Length > 0)
            {
                if (proofImage.Length > 5 * 1024 * 1024)
                    return Json(new { error = "File must be smaller than 5 MB." });

                var ext = Path.GetExtension(proofImage.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" }.Contains(ext))
                    return Json(new { error = "Only JPG, PNG, WEBP or PDF files are allowed." });

                var fileName = Guid.NewGuid().ToString() + ext;
                var path = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", "complaints", fileName);
                Directory.CreateDirectory(Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "complaints"));

                using var stream = new FileStream(path, FileMode.Create);
                await proofImage.CopyToAsync(stream);
                proofUrl = "/complaints/" + fileName;
            }

            complaint.SellerResponse = response;
            complaint.SellerProofImageUrl = proofUrl;
            complaint.Status = "SellerResponded";
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            SendEmail(
                "artisanvalley.store@gmail.com",
                $"Seller Responded — {WebUtility.HtmlEncode(complaint.TicketNumber)}",
                $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                    <h2 style='color:#a64d79;'>Seller Has Responded</h2>
                    <p><strong>Ticket:</strong> {WebUtility.HtmlEncode(complaint.TicketNumber)}</p>
                    <p><strong>Seller:</strong> {WebUtility.HtmlEncode(complaint.SellerName)}</p>
                    <p><strong>Response:</strong> {WebUtility.HtmlEncode(response)}</p>
                    <p>Please review and finalize this complaint in the admin panel.</p>
                </div>"
            );

            return Json(new { success = true });
        }

        private void SendEmail(string to, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley",
                    "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587,
                    MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com",
                    "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch { }
        }
    }
}
