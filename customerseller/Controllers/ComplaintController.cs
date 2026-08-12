using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MailKit.Net.Smtp;

namespace customerseller.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComplaintController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(
            string customerName, string customerPhone, string customerEmail,
            string orderId, string amountPaid, string sellerName,
            string complaintType, string priority, string description,
            IFormFile? evidenceFile)
        {
            // Ticket number generate karo
            var ticket = "CMP-" + new Random().Next(1000, 9999).ToString();

            string? imageUrl = null;

            // Evidence image save karo
            if (evidenceFile != null && evidenceFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() +
                               Path.GetExtension(evidenceFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", "complaints", fileName);
                Directory.CreateDirectory(Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "complaints"));

                using var stream = new FileStream(path, FileMode.Create);
                await evidenceFile.CopyToAsync(stream);
                imageUrl = "/complaints/" + fileName;
            }

            var complaint = new Complaint
            {
                TicketNumber = ticket,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                OrderId = orderId?.Replace("#", "").Trim(),
                AmountPaid = amountPaid,
                SellerName = sellerName,
                ComplaintType = complaintType,
                Priority = priority,
                Description = description,
                EvidenceImageUrl = imageUrl,
                Status = "Open",
                CreatedAt = DateTime.Now
            };

            _context.Complaints.Add(complaint);
            _context.SaveChanges();

            // Admin ko email bhejo
            SendEmail(
                "artisanvalley.store@gmail.com",
                $"New Complaint Received — {ticket}",
                $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                    <h2 style='color:#a64d79;'>New Complaint — {ticket}</h2>
                    <p><strong>Customer:</strong> {customerName} ({customerPhone})</p>
                    <p><strong>Seller:</strong> {sellerName}</p>
                    <p><strong>Type:</strong> {complaintType}</p>
                    <p><strong>Priority:</strong> {priority}</p>
                    <p><strong>Order ID:</strong> {orderId}</p>
                    <p><strong>Description:</strong> {description}</p>
                    <p>Login to admin panel to review.</p>
                </div>"
            );

            // Customer ko confirmation email bhejo
            if (!string.IsNullOrEmpty(customerEmail))
            {
                SendEmail(
                    customerEmail,
                    "Complaint Received — Artisan Valley",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                        <h2 style='color:#a64d79;'>Complaint Submitted ✅</h2>
                        <p>Dear {customerName},</p>
                        <p>Your complaint has been received.</p>
                        <p><strong>Ticket Number:</strong> {ticket}</p>
                        <p>We will review and respond within 24 hours.</p>
                        <p>Artisan Valley Team</p>
                    </div>"
                );
            }

            return Json(new { success = true, ticket = ticket });
        }

        // Admin complaint forward kare seller ko
        [HttpPost]
        public IActionResult ForwardToSeller(int complaintId, string adminNotes)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            complaint.Status = "Forwarded";
            complaint.AdminNotes = adminNotes;
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            // Seller ko email bhejo
            // Seller email Products table se dhundho
            var sellerEmail = _context.SellerShops
                .FirstOrDefault(s => s.ShopName.ToLower() ==
                                complaint.SellerName.ToLower())?.SellerEmail;

            if (!string.IsNullOrEmpty(sellerEmail))
            {
                SendEmail(
                    sellerEmail,
                    "Customer Complaint — Action Required",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                        <h2 style='color:#e65100;'>Complaint Forwarded to You</h2>
                        <p><strong>Ticket:</strong> {complaint.TicketNumber}</p>
                        <p><strong>Customer:</strong> {complaint.CustomerName}</p>
                        <p><strong>Type:</strong> {complaint.ComplaintType}</p>
                        <p><strong>Description:</strong> {complaint.Description}</p>
                        <p><strong>Admin Notes:</strong> {adminNotes}</p>
                        <p>Please login to your dashboard and respond to this complaint.</p>
                    </div>"
                );
            }

            return Json(new { success = true });
        }

        // Seller complaint ka response de
        [HttpPost]
        public async Task<IActionResult> SellerRespond(
            int complaintId, string response, IFormFile? proofImage)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            string? proofUrl = null;
            if (proofImage != null && proofImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() +
                               Path.GetExtension(proofImage.FileName);
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

            // Customer ko email bhejo
            if (!string.IsNullOrEmpty(complaint.CustomerEmail))
            {
                SendEmail(
                    complaint.CustomerEmail,
                    "Update on Your Complaint — Artisan Valley",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                        <h2 style='color:#a64d79;'>Complaint Update</h2>
                        <p>Ticket: <strong>{complaint.TicketNumber}</strong></p>
                        <p>The seller has responded to your complaint.</p>
                        <p><strong>Seller Response:</strong> {response}</p>
                        <p>Please login to confirm if your issue is resolved.</p>
                    </div>"
                );
            }

            return Json(new { success = true });
        }

        // Customer confirm kare — resolved ya nahi
        [HttpPost]
        public IActionResult CustomerConfirm(int complaintId, bool isResolved)
        {
            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            if (isResolved)
            {
                complaint.Status = "Resolved";
                complaint.IsResolvedByCustomer = true;
                complaint.ResolvedAt = DateTime.Now;
            }
            else
            {
                complaint.Status = "Escalated";
                complaint.IsResolvedByCustomer = false;
                complaint.UpdatedAt = DateTime.Now;
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // Admin fake mark kare
        [HttpPost]
        public IActionResult MarkFake(int complaintId)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            complaint.Status = "Fake";
            complaint.CustomerStrike += 1;
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        // Admin resolve kare
        [HttpPost]
        public IActionResult AdminResolve(int complaintId, string notes)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            complaint.Status = "Resolved";
            complaint.AdminNotes = notes;
            complaint.ResolvedAt = DateTime.Now;
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            // Customer ko email
            if (!string.IsNullOrEmpty(complaint.CustomerEmail))
            {
                SendEmail(
                    complaint.CustomerEmail,
                    "Your Complaint is Resolved — Artisan Valley",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                        <h2 style='color:#2e7d32;'>Complaint Resolved ✅</h2>
                        <p>Ticket: <strong>{complaint.TicketNumber}</strong></p>
                        <p>Your complaint has been resolved by admin.</p>
                        <p><strong>Notes:</strong> {notes}</p>
                        <p>Thank you for your patience.</p>
                    </div>"
                );
            }

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
        [HttpPost]
        public IActionResult DeleteComplaint(int complaintId)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            if (!string.IsNullOrEmpty(complaint.EvidenceImageUrl))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    complaint.EvidenceImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            if (!string.IsNullOrEmpty(complaint.SellerProofImageUrl))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    complaint.SellerProofImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Complaints.Remove(complaint);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}