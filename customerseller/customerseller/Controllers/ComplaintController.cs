using customerseller.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Net;
using System.Text.RegularExpressions;

namespace customerseller.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ComplaintController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet]
        public IActionResult Track(string ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket))
                return View("~/Views/Complaint/Track.cshtml", null);

            string searchTicket = ticket.Trim().ToUpper();

            var complaint = _context.Complaints
                .FirstOrDefault(c => c.TicketNumber.Trim().ToUpper() == searchTicket);

            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(sessionEmail))
                return RedirectToAction("Index", "Home", new { returnUrl = "/Complaint/Track?ticket=" + Uri.EscapeDataString(ticket) });
            if (_context.SellerShops.Any(s => s.SellerEmail.ToLower() == sessionEmail.ToLower()))
                return RedirectToAction("Index", "Home");
            if (complaint == null ||
                !string.Equals(complaint.CustomerEmail?.Trim(), sessionEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["TrackError"] = "No complaint found with this ticket on your account.";
                return View("~/Views/Complaint/Track.cshtml", null);
            }

            return View("~/Views/Complaint/Track.cshtml", complaint);
        }
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Submit(
    string customerName, string customerPhone, string customerEmail,
    string orderId, string amountPaid, string sellerName,
    string complaintType, string priority, string description,
    IFormFile? evidenceFile)
        {
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(sessionEmail))
                return Json(new { success = false, message = "Please login first." });

            if (HttpContext.Session.GetString("UserRole") == "Seller" ||
     _context.SellerShops.Any(s => s.SellerEmail.ToLower() == sessionEmail.ToLower()))
                return Json(new { success = false, message = "Sellers cannot file complaints." });
            customerEmail = sessionEmail;
            customerName = (customerName ?? "").Trim();
            customerPhone = (customerPhone ?? "").Trim();
            customerEmail = (customerEmail ?? "").Trim();
            orderId = (orderId ?? "").Replace("#", "").Trim();
            amountPaid = (amountPaid ?? "").Trim();
            sellerName = (sellerName ?? "").Trim();
            complaintType = (complaintType ?? "").Trim();
            description = (description ?? "").Trim();

            
            if (customerName.Length < 3 || customerName.Length > 50 ||
                !Regex.IsMatch(customerName, @"^[A-Za-z\s.'-]+$"))
                return Json(new { success = false, message = "Please enter a valid name (letters only, 3-50 characters)." });

            
            if (!Regex.IsMatch(customerPhone, @"^(\+92|0)3\d{9}$"))
                return Json(new { success = false, message = "Please enter a valid phone number (e.g. 03001234567)." });

            
            if (!string.IsNullOrEmpty(customerEmail) &&
                (customerEmail.Length > 100 || !Regex.IsMatch(customerEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")))
                return Json(new { success = false, message = "Please enter a valid email address." });

            
            if (!Regex.IsMatch(orderId, @"^[A-Za-z0-9-]{3,30}$"))
                return Json(new { success = false, message = "Please enter a valid Order ID." });

            var verifiedOrder = _context.Orders
     .Include(o => o.Items)
     .AsNoTracking()
     .FirstOrDefault(o => o.OrderId.ToUpper() == orderId.ToUpper());

           
            if (verifiedOrder == null ||
                !string.Equals(verifiedOrder.Email?.Trim(), sessionEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "No order found with this ID on your account." });

            if (verifiedOrder.Status != "Delivered")
                return Json(new { success = false, message = "You can only file a complaint on a delivered order." });

            orderId = verifiedOrder.OrderId;
            sellerName = verifiedOrder.Items?.FirstOrDefault()?.SellerShopName ?? sellerName;
            amountPaid = verifiedOrder.Total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(amountPaid))
            {
                if (!decimal.TryParse(amountPaid, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var amt) || amt <= 0 || amt > 10000000)
                    return Json(new { success = false, message = "Please enter a valid amount." });
            }

            
            if (sellerName.Length < 2 || sellerName.Length > 80 ||
                !Regex.IsMatch(sellerName, @"^[A-Za-z0-9\s.'&-]+$"))
                return Json(new { success = false, message = "Please enter a valid seller / shop name." });

            
            var allowedTypes = new[]
            {
        "Wrong / Damaged product received", "Product never arrived", "Delivery was very late",
        "Seller fraud / scam", "Rude or unprofessional seller",
        "Return / Refund not processed", "Other issue"
    };
            if (!allowedTypes.Contains(complaintType))
                return Json(new { success = false, message = "Please select a valid complaint type." });

            
            if (description.Length < 20 || description.Length > 1000)
                return Json(new { success = false, message = "Description must be between 20 and 1000 characters." });

            
            priority = "Medium";

            
            if (evidenceFile == null || evidenceFile.Length == 0)
                return Json(new { success = false, message = "Please attach a photo/screenshot as evidence." });

            if (evidenceFile.Length > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File must be smaller than 5 MB." });

            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            var ext = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                return Json(new { success = false, message = "Only JPG, PNG, WEBP or PDF files are allowed." });


            string ticket;
            do
            {
                ticket = "CMP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            } while (_context.Complaints.Any(c => c.TicketNumber == ticket));

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "complaints");
            Directory.CreateDirectory(folder);
            var fileName = Guid.NewGuid().ToString() + ext;
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await evidenceFile.CopyToAsync(stream);
            }
            var imageUrl = "/complaints/" + fileName;

            var complaint = new Complaint
            {
                TicketNumber = ticket,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = string.IsNullOrEmpty(customerEmail) ? null : customerEmail,
                OrderId = orderId,
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


            var eName = WebUtility.HtmlEncode(customerName);
            var ePhone = WebUtility.HtmlEncode(customerPhone);
            var eSeller = WebUtility.HtmlEncode(sellerName);
            var eType = WebUtility.HtmlEncode(complaintType);
            var eOrder = WebUtility.HtmlEncode(orderId);
            var eDesc = WebUtility.HtmlEncode(description);

            SendEmail(
                _config["EmailSettings:FromEmail"],
                $"New Complaint Received — {ticket}",
                $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
            <h2 style='color:#a64d79;'>New Complaint — {ticket}</h2>
            <p><strong>Customer:</strong> {eName} ({ePhone})</p>
            <p><strong>Seller:</strong> {eSeller}</p>
            <p><strong>Type:</strong> {eType}</p>
            <p><strong>Priority:</strong> {priority}</p>
            <p><strong>Order ID:</strong> {eOrder}</p>
            <p><strong>Description:</strong> {eDesc}</p>
            <p>Login to admin panel to review.</p>
        </div>"
            );

            if (!string.IsNullOrEmpty(customerEmail))
            {
                SendEmail(
                    customerEmail,
                    "Complaint Received — Artisan Valley",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                <h2 style='color:#a64d79;'>Complaint Submitted ✅</h2>
                <p>Dear {eName},</p>
                <p>Your complaint has been received.</p>
                <p><strong>Ticket Number:</strong> {ticket}</p>
                <p>We will review and respond within 24 hours.</p>
                <p>Artisan Valley Team</p>
            </div>"
                );
            }

            return Json(new { success = true, ticket = ticket });
        }




        [HttpPost]
        public IActionResult CustomerConfirm(int complaintId, bool isResolved)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { error = "Unauthorized" });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            if (!string.Equals(complaint.CustomerEmail, userEmail, StringComparison.OrdinalIgnoreCase))
                return Json(new { error = "You are not authorized to update this complaint." });

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


        [HttpPost]
        public IActionResult MarkFake(int complaintId, string? notes)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            notes = (notes ?? "").Trim();
            if (notes.Length > 1000)
                return Json(new { error = "Notes cannot exceed 1000 characters." });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            if (complaint.Status == "Fake")
                return Json(new { error = "This complaint is already marked as fake." });

            if (complaint.Status == "Resolved")
                return Json(new { error = "A resolved complaint cannot be marked as fake." });

            complaint.Status = "Fake";
            if (!string.IsNullOrEmpty(notes)) complaint.AdminNotes = notes;
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

           
            var email = complaint.CustomerEmail;
            var phone = complaint.CustomerPhone;
            int totalStrikes = !string.IsNullOrEmpty(email)
                ? _context.Complaints.Count(c => c.Status == "Fake" && c.CustomerEmail == email)
                : _context.Complaints.Count(c => c.Status == "Fake" && c.CustomerPhone == phone);

            complaint.CustomerStrike = totalStrikes;

           
            if (totalStrikes >= 3 && !string.IsNullOrEmpty(email))
            {
                var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
                if (user != null && !user.IsBlocked)
                {
                    user.IsBlocked = true;
                    user.BlockReason = "Multiple fake complaints";
                    user.BlockExpiry = DateTime.Now.AddDays(7);
                }
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ForwardToSeller(int complaintId, string adminNotes)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            adminNotes = (adminNotes ?? "").Trim();
            if (adminNotes.Length > 1000)
                return Json(new { error = "Notes cannot exceed 1000 characters." });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });
            if (complaint.Status == "Fake" || complaint.Status == "Resolved")
                return Json(new { error = "This complaint is already closed." });
            complaint.Status = "Forwarded";
            complaint.AdminNotes = adminNotes;
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

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
                <p><strong>Ticket:</strong> {WebUtility.HtmlEncode(complaint.TicketNumber)}</p>
                <p><strong>Customer:</strong> {WebUtility.HtmlEncode(complaint.CustomerName)}</p>
                <p><strong>Type:</strong> {WebUtility.HtmlEncode(complaint.ComplaintType ?? "")}</p>
                <p><strong>Description:</strong> {WebUtility.HtmlEncode(complaint.Description)}</p>
                <p><strong>Admin Notes:</strong> {WebUtility.HtmlEncode(adminNotes)}</p>
                <p>Please login to your dashboard and respond to this complaint.</p>
            </div>"
                );
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> SellerRespond(
            int complaintId, string response, IFormFile? proofImage)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { error = "Unauthorized" });

            response = (response ?? "").Trim();
            if (response.Length < 10 || response.Length > 1000)
                return Json(new { error = "Response must be between 10 and 1000 characters." });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            var myShop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail.ToLower() == email.ToLower());
            if (myShop == null || !string.Equals(myShop.ShopName, complaint.SellerName, StringComparison.OrdinalIgnoreCase))
                return Json(new { error = "You are not authorized to respond to this complaint." });

            string? proofUrl = null;
            if (proofImage != null && proofImage.Length > 0)
            {
                if (proofImage.Length > 5 * 1024 * 1024)
                    return Json(new { error = "File must be smaller than 5 MB." });

                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
                var ext = Path.GetExtension(proofImage.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext))
                    return Json(new { error = "Only JPG, PNG, WEBP or PDF files are allowed." });

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "complaints");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid().ToString() + ext;

                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await proofImage.CopyToAsync(stream);
                }
                proofUrl = "/complaints/" + fileName;
            }

            complaint.SellerResponse = response;
            complaint.SellerProofImageUrl = proofUrl;
            complaint.Status = "SellerResponded";
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            if (!string.IsNullOrEmpty(complaint.CustomerEmail))
            {
                SendEmail(
                    complaint.CustomerEmail,
                    "Update on Your Complaint — Artisan Valley",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                <h2 style='color:#a64d79;'>Complaint Update</h2>
                <p>Ticket: <strong>{WebUtility.HtmlEncode(complaint.TicketNumber)}</strong></p>
                <p>The seller has responded to your complaint.</p>
                <p><strong>Seller Response:</strong> {WebUtility.HtmlEncode(response)}</p>
                <p>Please login to confirm if your issue is resolved.</p>
            </div>"
                );
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult AdminResolve(int complaintId, string notes)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            notes = (notes ?? "").Trim();
            if (notes.Length > 1000)
                return Json(new { error = "Notes cannot exceed 1000 characters." });

            var complaint = _context.Complaints.Find(complaintId);
            if (complaint == null) return Json(new { error = "Not found" });

            if (complaint.Status == "Fake" || complaint.Status == "Resolved")
                return Json(new { error = "This complaint is already closed." });

            complaint.Status = "Resolved";
            complaint.AdminNotes = notes;
            complaint.ResolvedAt = DateTime.Now;
            complaint.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            if (!string.IsNullOrEmpty(complaint.CustomerEmail))
            {
                SendEmail(
                    complaint.CustomerEmail,
                    "Your Complaint is Resolved — Artisan Valley",
                    $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                <h2 style='color:#2e7d32;'>Complaint Resolved ✅</h2>
                <p>Ticket: <strong>{WebUtility.HtmlEncode(complaint.TicketNumber)}</strong></p>
                <p>Your complaint has been resolved by admin.</p>
                <p><strong>Notes:</strong> {WebUtility.HtmlEncode(notes)}</p>
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
                var fromEmail = _config["EmailSettings:FromEmail"];
                var appPassword = _config["EmailSettings:AppPassword"];
                var smtpHost = _config["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                client.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(fromEmail, appPassword);
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

            if (complaint.Status == "Fake")
                return Json(new { error = "Fake complaints cannot be deleted because they count as customer strikes." });

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