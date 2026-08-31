using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace customerseller.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Pay()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var lastSubmission = _context.PaymentSubmissions
                .Where(p => p.SellerEmail == email)
                .OrderByDescending(p => p.SubmittedAt)
                .FirstOrDefault();

            ViewBag.LastSubmission = lastSubmission;

            using var con = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT SubscriptionEnd FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", email);
            var result = cmd.ExecuteScalar();

            DateTime? subEnd = result != null && result != DBNull.Value ? (DateTime?)result : null;
            bool isActive = subEnd.HasValue && subEnd.Value > DateTime.Now;

            ViewBag.IsActive = isActive;
            ViewBag.SubscriptionEnd = subEnd;

            return View();
        }

        [HttpPost]
        public IActionResult SubmitPayment(string transactionId, string paymentMethod, string senderNumber, IFormFile screenshot)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");
            using (var conCheck = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString()))
            {
                conCheck.Open();
                var checkCmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT SubscriptionEnd FROM SellerShops WHERE SellerEmail = @email", conCheck);
                checkCmd.Parameters.AddWithValue("@email", email);
                var res = checkCmd.ExecuteScalar();
                DateTime? subEnd = res != null && res != DBNull.Value ? (DateTime?)res : null;

                if (subEnd.HasValue && subEnd.Value > DateTime.Now)
                {
                    TempData["PaymentError"] = "You have already paid for this month. Your subscription is active until " + subEnd.Value.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) + ".";
                    return RedirectToAction("Pay");
                }
            }

            transactionId = transactionId?.Trim() ?? "";
            senderNumber = senderNumber?.Trim().Replace("-", "") ?? "";
            paymentMethod = (paymentMethod == "Easypaisa") ? "Easypaisa" : "JazzCash";

          
            if (!System.Text.RegularExpressions.Regex.IsMatch(transactionId, @"^[0-9]{11}$"))
            {
                TempData["PaymentError"] = "Transaction ID must be exactly 11 digits (as shown on your receipt).";
                return RedirectToAction("Pay");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(senderNumber, @"^03\d{9}$"))
            {
                TempData["PaymentError"] = "Sender number must be in the format 03XXXXXXXXX.";
                return RedirectToAction("Pay");
            }

            if (screenshot == null || screenshot.Length == 0)
            {
                TempData["PaymentError"] = "Please upload a screenshot of your payment receipt.";
                return RedirectToAction("Pay");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(screenshot.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["PaymentError"] = "Screenshot must be a JPG, PNG, or WEBP image.";
                return RedirectToAction("Pay");
            }

            if (screenshot.Length > 5 * 1024 * 1024)
            {
                TempData["PaymentError"] = "Screenshot must be under 5MB.";
                return RedirectToAction("Pay");
            }

            bool alreadyUsed = _context.PaymentSubmissions.Any(p => p.TransactionId == transactionId);
            if (alreadyUsed)
            {
                TempData["PaymentError"] = "This Transaction ID has already been submitted. Please check and try again.";
                return RedirectToAction("Pay");
            }

            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payments");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                screenshot.CopyTo(stream);
            }

            var submission = new PaymentSubmission
            {
                SellerEmail = email,
                TransactionId = transactionId,
                Amount = 1000,  
                SenderNumber = senderNumber,
                PaymentMethod = paymentMethod,
                ScreenshotPath = "/uploads/payments/" + fileName,
                Status = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.PaymentSubmissions.Add(submission);
            _context.SaveChanges();

            TempData["PaymentSuccess"] = "Payment submitted! It will be reviewed within 24 hours.";
            return RedirectToAction("Pay");
        }


    }
}