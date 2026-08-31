using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MailKit.Net.Smtp;

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

           
            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);
            string sellerName = shop?.ShopName ?? "";

            var complaints = _context.Complaints
                .Where(c => c.SellerName.ToLower() == sellerName.ToLower())
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View(complaints);
        }

        public IActionResult GetCount()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { count = 0 });

            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);
            string sellerName = shop?.ShopName ?? "";

            var count = _context.Complaints
                .Count(c => c.SellerName.ToLower() == sellerName.ToLower()
                       && c.Status == "Forwarded");

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

            string? proofUrl = null;
            if (proofImage != null && proofImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(proofImage.FileName);
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
                $"Seller Responded — {complaint.TicketNumber}",
                $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                    <h2 style='color:#a64d79;'>Seller Has Responded</h2>
                    <p><strong>Ticket:</strong> {complaint.TicketNumber}</p>
                    <p><strong>Seller:</strong> {complaint.SellerName}</p>
                    <p><strong>Response:</strong> {response}</p>
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
