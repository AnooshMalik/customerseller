using customerseller.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace customerseller.Controllers
{
    public class CourierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourierController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("CourierEmail") != null)
                return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var courier = _context.CourierCompanies
                .FirstOrDefault(c => c.Email.ToLower() == email.Trim().ToLower());

            if (courier == null || courier.Password != password.Trim())
            {
                TempData["CourierError"] = "Invalid email or password.";
                return RedirectToAction("Login");
            }

            if (!courier.IsActive)
            {
                TempData["CourierError"] = "Your account has been deactivated. Contact admin.";
                return RedirectToAction("Login");
            }

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("CourierOtp", otp);
            HttpContext.Session.SetString("CourierOtpExpiry", DateTime.Now.AddMinutes(5).ToString());
            HttpContext.Session.SetString("CourierEmailPending", courier.Email);

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", courier.Email));
                message.Subject = "Rider Login OTP";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Rider Login OTP</h2>
                    <p>Your OTP is: <strong style='font-size:24px; color:#a64d79;'>{otp}</strong></p>
                    <p>This OTP expires in 5 minutes.</p>"
                };

                using var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                TempData["CourierError"] = "Email error: " + ex.Message;
                return RedirectToAction("Login");
            }

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (HttpContext.Session.GetString("CourierEmailPending") == null)
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOtp(string otp)
        {
            var savedOtp = HttpContext.Session.GetString("CourierOtp");
            var expiryStr = HttpContext.Session.GetString("CourierOtpExpiry");
            var pendingEmail = HttpContext.Session.GetString("CourierEmailPending");

            if (savedOtp == null || expiryStr == null || pendingEmail == null)
            {
                TempData["CourierError"] = "Session expired. Please login again.";
                return RedirectToAction("Login");
            }

            if (DateTime.Now > DateTime.Parse(expiryStr))
            {
                TempData["CourierError"] = "OTP expired. Please login again.";
                return RedirectToAction("Login");
            }

            if (otp != savedOtp)
            {
                TempData["OtpError"] = "Invalid OTP. Please try again.";
                return View();
            }

            var courier = _context.CourierCompanies.FirstOrDefault(c => c.Email == pendingEmail);
            if (courier == null)
            {
                TempData["CourierError"] = "Account not found.";
                return RedirectToAction("Login");
            }
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("CourierEmail", courier.Email);
            HttpContext.Session.SetString("CourierName", courier.CompanyName);
            HttpContext.Session.SetInt32("CourierId", courier.Id);

            HttpContext.Session.Remove("CourierOtp");
            HttpContext.Session.Remove("CourierOtpExpiry");
            HttpContext.Session.Remove("CourierEmailPending");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [HttpPost]
        public IActionResult ResendCourierOtp()
        {
            var pendingEmail = HttpContext.Session.GetString("CourierEmailPending");
            if (string.IsNullOrEmpty(pendingEmail))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var courier = _context.CourierCompanies.FirstOrDefault(c => c.Email == pendingEmail);
            if (courier == null)
                return Json(new { success = false, message = "Account not found." });

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("CourierOtp", otp);
            HttpContext.Session.SetString("CourierOtpExpiry", DateTime.Now.AddMinutes(5).ToString());

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", courier.Email));
                message.Subject = "Rider Login OTP (Resent)";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Rider Login OTP</h2>
                    <p>Your new OTP is: <strong style='font-size:24px; color:#a64d79;'>{otp}</strong></p>
                    <p>This OTP expires in 5 minutes.</p>"
                };

                using var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Email error: " + ex.Message });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ForgotPasswordAjax(string email)
        {
            var courier = _context.CourierCompanies.FirstOrDefault(c =>
                c.Email.ToLower() == email.Trim().ToLower());

            if (courier == null)
                return Json(new { success = false, message = "No rider account found with this email." });

            var token = Guid.NewGuid().ToString();
            courier.ResetToken = token;
            courier.ResetTokenExpiry = DateTime.Now.AddHours(1);
            _context.SaveChanges();

            string resetLink = $"{Request.Scheme}://{Request.Host}/Courier/ResetPassword?token={token}";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", courier.Email));
                message.Subject = "Rider Password Reset";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Rider Password Reset Request</h2>
                    <p>Click the link below to reset your password:</p>
                    <a href='{resetLink}' style='background:#a64d79; color:#fff; padding:12px 24px; border-radius:8px; text-decoration:none;'>Reset Password</a>
                    <p>This link expires in 1 hour.</p>"
                };
                using var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Email error: " + ex.Message });
            }

            return Json(new { success = true, message = "Reset link sent to your email!" });
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var courier = _context.CourierCompanies.FirstOrDefault(c =>
                c.ResetToken == token && c.ResetTokenExpiry > DateTime.Now);

            if (courier == null)
            {
                TempData["CourierError"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            var courier = _context.CourierCompanies.FirstOrDefault(c =>
                c.ResetToken == token && c.ResetTokenExpiry > DateTime.Now);

            if (courier == null)
            {
                TempData["CourierError"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            courier.Password = newPassword;
            courier.ResetToken = null;
            courier.ResetTokenExpiry = null;
            _context.SaveChanges();

            TempData["CourierError"] = "Password reset successful! Please login.";
            return RedirectToAction("Login");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("CourierEmail");
            HttpContext.Session.Remove("CourierName");
            HttpContext.Session.Remove("CourierId");
            return RedirectToAction("Login");
        }

       
        [HttpGet]
        public IActionResult Dashboard()
        {
            var courierId = HttpContext.Session.GetInt32("CourierId");
            if (courierId == null)
                return RedirectToAction("Login");

            var orders = _context.Orders
                .Where(o => o.CourierCompanyId == courierId)
                .OrderByDescending(o => o.CourierAssignedAt)
                .ToList();

            ViewBag.Pending = orders.Count(o => o.CourierStatus == "Assigned" || o.CourierStatus == "PickedUp");
            ViewBag.Delivered = orders.Count(o => o.CourierStatus == "Delivered");
            ViewBag.Total = orders.Count;

            return View(orders);
        }

        [HttpPost]
        public IActionResult MarkPickedUp(string orderId)
        {
            var courierId = HttpContext.Session.GetInt32("CourierId");
            if (courierId == null) return Json(new { error = "Unauthorized" });

            var order = _context.Orders
                .Include(o => o.TrackingTimeline)
                .FirstOrDefault(o => o.OrderId == orderId && o.CourierCompanyId == courierId);
            if (order == null) return Json(new { error = "Not found" });

            order.CourierStatus = "PickedUp";
            order.Status = "Shipped";
            order.LastUpdatedAt = DateTime.Now;

            
            var shippedStep = order.TrackingTimeline?.FirstOrDefault(s => s.Status == "Shipped");
            if (shippedStep != null)
            {
                shippedStep.Completed = true;
                shippedStep.Active = true;
                shippedStep.Timestamp = DateTime.Now;
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult MarkDelivered(string orderId, bool paymentReceived, string? reason)
        {
            var courierId = HttpContext.Session.GetInt32("CourierId");
            if (courierId == null) return Json(new { error = "Unauthorized" });

            var order = _context.Orders
                .Include(o => o.TrackingTimeline)
                .FirstOrDefault(o => o.OrderId == orderId && o.CourierCompanyId == courierId);
            if (order == null) return Json(new { error = "Not found" });

            order.CourierStatus = "Delivered";
            order.DeliveredAt = DateTime.Now;
            order.LastUpdatedAt = DateTime.Now;
            order.PaymentReceived = paymentReceived;
            order.PaymentNotReceivedReason = paymentReceived ? null : reason;
            order.Status = paymentReceived ? "Delivered" : "Delivered - Payment Issue";

           
            var outForDelivery = order.TrackingTimeline?.FirstOrDefault(s => s.Status == "Out for Delivery");
            if (outForDelivery != null)
            {
                outForDelivery.Completed = true;
                outForDelivery.Active = true;
                outForDelivery.Timestamp = DateTime.Now;
            }

            var deliveredStep = order.TrackingTimeline?.FirstOrDefault(s => s.Status == "Delivered");
            if (deliveredStep != null)
            {
                deliveredStep.Completed = true;
                deliveredStep.Active = false;
                deliveredStep.Timestamp = DateTime.Now;
            }

            _context.SaveChanges();

           
            try
            {
                var statusMsg = paymentReceived
                    ? "Payment has been collected successfully."
                    : $"Payment was NOT collected. Reason: {reason}";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", order.Email));
                message.Subject = $"Order Delivered — #{order.OrderId}";
                message.Body = new TextPart("html")
                {
                    Text = $@"<div style='font-family:sans-serif;'>
                <h2 style='color:#a64d79;'>Order Delivered</h2>
                <p>Order <strong>#{order.OrderId}</strong> has been delivered.</p>
                <p>{statusMsg}</p>
            </div>"
                };
                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch { }

            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult CancelOrderByRider(string orderId, string reason)
        {
            var courierId = HttpContext.Session.GetInt32("CourierId");
            if (courierId == null) return Json(new { error = "Unauthorized" });

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.CourierCompanyId == courierId);
            if (order == null) return Json(new { error = "Not found" });

            order.CourierStatus = "Cancelled";
            order.Status = "Cancelled";
            order.CancelledDate = DateTime.Now;
            order.LastUpdatedAt = DateTime.Now;
            order.PaymentReceived = false;
            order.PaymentNotReceivedReason = reason;

            _context.SaveChanges();

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", order.Email));
                message.Subject = $"Order Cancelled — #{order.OrderId}";
                message.Body = new TextPart("html")
                {
                    Text = $@"<div style='font-family:sans-serif;'>
                <h2 style='color:#c62828;'>Order Cancelled</h2>
                <p>Order <strong>#{order.OrderId}</strong> was cancelled during delivery.</p>
                <p>Reason: {reason}</p>
            </div>"
                };
                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch { }

            return Json(new { success = true });
        }
        public IActionResult OrderDetails(string orderId)
        {
            var courierId = HttpContext.Session.GetInt32("CourierId");
            if (courierId == null) return RedirectToAction("Login");

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.CourierCompanyId == courierId);
            if (order == null) return NotFound();

            return View(order);
        }
    }
}
