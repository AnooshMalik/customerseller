using customerseller.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace customerseller.Controllers
{
    public class ModeratorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string AdminEmail = "artisanvalley.store@gmail.com";

        public ModeratorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("ModeratorRole") != null)
                return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var mod = _context.ModeratorSettings
                .FirstOrDefault(m => m.Email.ToLower() == email.Trim().ToLower());

            if (mod != null && mod.Password == password.Trim())
            {
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("ModOtp", otp);
                HttpContext.Session.SetString("ModOtpExpiry",
                    DateTime.Now.AddMinutes(5).ToString());
                HttpContext.Session.SetString("ModEmailPending", email);

                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("Artisan Valley",
                        "artisanvalley.store@gmail.com"));
                    message.To.Add(new MailboxAddress("", email));
                    message.Subject = "Moderator Login OTP";
                    message.Body = new TextPart("html")
                    {
                        Text = $@"<h2>Login OTP</h2>
                        <p>Your OTP: <strong style='font-size:24px;
                        color:#a64d79;'>{otp}</strong></p>
                        <p>Expires in 5 minutes.</p>"
                    };
                    using var client = new SmtpClient();
                    client.Connect("smtp.gmail.com", 587,
                        MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    TempData["ModError"] = "Email error: " + ex.Message;
                    return View();
                }

                return RedirectToAction("Otp");
            }

            TempData["ModError"] = "Invalid email or password.";
            return View();
        }

        [HttpGet]
        public IActionResult Otp()
        {
            if (HttpContext.Session.GetString("ModEmailPending") == null)
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public IActionResult Otp(string otp)
        {
            var savedOtp = HttpContext.Session.GetString("ModOtp");
            var expiryStr = HttpContext.Session.GetString("ModOtpExpiry");

            if (savedOtp == null || expiryStr == null)
            {
                TempData["ModError"] = "Session expired.";
                return RedirectToAction("Login");
            }

            if (DateTime.Now > DateTime.Parse(expiryStr))
            {
                TempData["ModError"] = "OTP expired.";
                return RedirectToAction("Login");
            }

            if (otp == savedOtp)
            {
                string email = HttpContext.Session.GetString("ModEmailPending");   
                string role = (email.ToLower() == AdminEmail.ToLower()) ? "Admin" : "Moderator";

                HttpContext.Session.Clear();                                      
                HttpContext.Session.SetString("ModeratorEmail", email);
                HttpContext.Session.SetString("ModeratorRole", role);
                return RedirectToAction("Dashboard");
            }
            TempData["OtpError"] = "Invalid OTP.";
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPasswordAjax(string email)
        {
            var mod = _context.ModeratorSettings.FirstOrDefault(m =>
                m.Email.ToLower() == email.Trim().ToLower());

            if (mod == null)
                return Json(new { success = false, message = "No moderator account found with this email." });

            var token = Guid.NewGuid().ToString();
            mod.ResetToken = token;
            mod.ResetTokenExpiry = DateTime.Now.AddHours(1);
            _context.SaveChanges();

            string resetLink = $"{Request.Scheme}://{Request.Host}/Moderator/ResetPassword?token={token}";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", mod.Email));
                message.Subject = "Moderator Password Reset";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Moderator Password Reset Request</h2>
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
            var mod = _context.ModeratorSettings.FirstOrDefault(m =>
                m.ResetToken == token && m.ResetTokenExpiry > DateTime.Now);

            if (mod == null)
            {
                TempData["ModError"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            var mod = _context.ModeratorSettings.FirstOrDefault(m =>
                m.ResetToken == token && m.ResetTokenExpiry > DateTime.Now);

            if (mod == null)
            {
                TempData["ModError"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            mod.Password = newPassword;
            mod.ResetToken = null;
            mod.ResetTokenExpiry = null;
            _context.SaveChanges();

            TempData["ModError"] = "Password reset successful! Please login.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult ResendOtp()
        {
            var pendingEmail = HttpContext.Session.GetString("ModEmailPending");
            if (string.IsNullOrEmpty(pendingEmail))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var mod = _context.ModeratorSettings.FirstOrDefault(m =>
                m.Email.ToLower() == pendingEmail.ToLower());
            if (mod == null)
                return Json(new { success = false, message = "Moderator account not found." });

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ModOtp", otp);
            HttpContext.Session.SetString("ModOtpExpiry",
                DateTime.Now.AddMinutes(5).ToString());

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", mod.Email));
                message.Subject = "Moderator Login OTP (Resent)";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Login OTP</h2>
                    <p>Your new OTP: <strong style='font-size:24px; color:#a64d79;'>{otp}</strong></p>
                    <p>Expires in 5 minutes.</p>"
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
        [HttpGet]
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("ModeratorRole") == null)
                return RedirectToAction("Login");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
    SELECT v.*, 
           (SELECT COUNT(*) FROM Products p 
            WHERE p.SellerEmail = v.SellerEmail 
            AND p.Category = v.SubCategory) AS ProductCount,
           (SELECT STRING_AGG(p.Title, ', ') FROM Products p 
            WHERE p.SellerEmail = v.SellerEmail 
            AND p.Category = v.SubCategory) AS ProductTitles
    FROM SubCategoryVideos v 
    WHERE v.VideoStatus = 'Pending'", con);


            var reader = cmd.ExecuteReader();
            var pending = new List<dynamic>();

            while (reader.Read())
            {
                pending.Add(new
                {
                    Id = reader["Id"],
                    SellerEmail = reader["SellerEmail"],
                    SubCategory = reader["SubCategory"],
                    VideoUrl = reader["VideoUrl"],
                    VideoStatus = reader["VideoStatus"],
                    UploadedAt = reader["UploadedAt"],

                    ProductCount = reader["ProductCount"],   
                    ProductTitles = reader["ProductTitles"]
                });
            }
            reader.Close();

            ViewBag.PendingVideos = pending;
            ViewBag.PendingCount = pending.Count;

            return View();
        }
        [HttpPost]
        public IActionResult Approve(int videoId, string internalNotes)
        {
            if (HttpContext.Session.GetString("ModeratorRole") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

           
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE SubCategoryVideos SET VideoStatus = 'Approved' WHERE Id = @id", con);
            cmd.Parameters.AddWithValue("@id", videoId);
            cmd.ExecuteNonQuery();

            
            var subCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SellerEmail, SubCategory FROM SubCategoryVideos WHERE Id = @id", con);
            subCmd.Parameters.AddWithValue("@id", videoId);
            var reader = subCmd.ExecuteReader();
            string sellerEmail = "", subCategory = "";
            if (reader.Read())
            {
                sellerEmail = reader["SellerEmail"].ToString();
                subCategory = reader["SubCategory"].ToString();
            }
            reader.Close();

            var updateProducts = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE Products SET VideoStatus = 'Approved', ReviewedAt = @now, ReviewedBy = @by WHERE SellerEmail = @email AND Category = @sub",
                con);
            updateProducts.Parameters.AddWithValue("@now", DateTime.Now);
            updateProducts.Parameters.AddWithValue("@by", HttpContext.Session.GetString("ModeratorEmail"));
            updateProducts.Parameters.AddWithValue("@email", sellerEmail);
            updateProducts.Parameters.AddWithValue("@sub", subCategory);
            updateProducts.ExecuteNonQuery();

            
            var updateShop = new Microsoft.Data.SqlClient.SqlCommand(
                @"UPDATE SellerShops 
      SET ModeratorStatus = 'Approved', 
          ModeratorEmail = @modEmail,
          ModeratorReviewedAt = @now
      WHERE SellerEmail = @email", con);
            updateShop.Parameters.AddWithValue("@modEmail", HttpContext.Session.GetString("ModeratorEmail"));
            updateShop.Parameters.AddWithValue("@now", DateTime.Now);
            updateShop.Parameters.AddWithValue("@email", sellerEmail);
            updateShop.ExecuteNonQuery();

           
            SendEmail(sellerEmail, "Making Video Approved — Artisan Valley",
                $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
            <h2 style='color:#a64d79;'>Your Making Video is Approved! ✅</h2>
            <p>Your making video for sub-category <strong>{subCategory}</strong> has been approved by our moderator.</p>
            <p>Your products in this category are now under admin review.</p>
        </div>");



            return Json(new { success = true });
        }

        private void SyncShopModeratorStatus(Microsoft.Data.SqlClient.SqlConnection con, string sellerEmail)
        {
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT VideoStatus FROM SubCategoryVideos WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", sellerEmail);

            var statuses = new List<string>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    statuses.Add(reader["VideoStatus"].ToString());
            }

            string aggregateStatus;
            if (statuses.Any(s => s == "Approved"))
                aggregateStatus = "Approved";
            else if (statuses.Count > 0 && statuses.All(s => s == "Rejected"))
                aggregateStatus = "Rejected";
            else
                aggregateStatus = "Pending";

            var updateShop = new Microsoft.Data.SqlClient.SqlCommand(
                @"UPDATE SellerShops 
          SET ModeratorStatus = @status,
              ModeratorEmail = @modEmail,
              ModeratorReviewedAt = @now
          WHERE SellerEmail = @email", con);
            updateShop.Parameters.AddWithValue("@status", aggregateStatus);
            updateShop.Parameters.AddWithValue("@modEmail", HttpContext.Session.GetString("ModeratorEmail"));
            updateShop.Parameters.AddWithValue("@now", DateTime.Now);
            updateShop.Parameters.AddWithValue("@email", sellerEmail);
            updateShop.ExecuteNonQuery();
        }

        [HttpPost]
        public IActionResult Reject(int videoId, string reason)
        {
            if (HttpContext.Session.GetString("ModeratorRole") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE SubCategoryVideos SET VideoStatus = 'Rejected', RejectionReason = @reason WHERE Id = @id", con);
            cmd.Parameters.AddWithValue("@reason", reason ?? "No reason provided");
            cmd.Parameters.AddWithValue("@id", videoId);
            cmd.ExecuteNonQuery();

            var getCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SellerEmail, SubCategory FROM SubCategoryVideos WHERE Id = @id", con);
            getCmd.Parameters.AddWithValue("@id", videoId);
            var reader = getCmd.ExecuteReader();
            string sellerEmail = "", subCategory = "";
            if (reader.Read())
            {
                sellerEmail = reader["SellerEmail"].ToString();
                subCategory = reader["SubCategory"].ToString();
            }
            reader.Close();

            SyncShopModeratorStatus(con, sellerEmail);

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", sellerEmail));
                message.Subject = "Making Video Rejected — Artisan Valley";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"
<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
    <h2 style='color:#dc2626;'>Making Video Rejected ❌</h2>
    <p>Your making video for <strong>{subCategory}</strong> has been rejected.</p>
    <p><strong>Reason:</strong> {reason ?? "No reason provided"}</p>
    <p>Please upload a new video for this sub-category.</p>
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
        public IActionResult GetStats()
        {
            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var pendingCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT COUNT(*) FROM SubCategoryVideos WHERE VideoStatus = 'Pending'", con);
            var approvedCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT COUNT(*) FROM SubCategoryVideos WHERE VideoStatus = 'Approved'", con);
            var rejectedCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT COUNT(*) FROM SubCategoryVideos WHERE VideoStatus = 'Rejected'", con);

            var stats = new
            {
                pending = (int)pendingCmd.ExecuteScalar(),
                approved = (int)approvedCmd.ExecuteScalar(),
                rejected = (int)rejectedCmd.ExecuteScalar()
            };

            return Json(stats);
        }

        public IActionResult GetVideoHistory(string status)
        {
            if (HttpContext.Session.GetString("ModeratorRole") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT * FROM SubCategoryVideos WHERE VideoStatus = @status", con);
            cmd.Parameters.AddWithValue("@status", status);

            con.Open();
            var reader = cmd.ExecuteReader();
            var videos = new List<object>();

            while (reader.Read())
            {
                videos.Add(new
                {
                    Id = reader["Id"],
                    SellerEmail = reader["SellerEmail"],
                    SubCategory = reader["SubCategory"],
                    VideoUrl = reader["VideoUrl"],
                    VideoStatus = reader["VideoStatus"],
                    UploadedAt = reader["UploadedAt"],
                    RejectionReason = reader["RejectionReason"] == DBNull.Value ? "" : reader["RejectionReason"].ToString()
                });
            }

            return Json(videos);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("ModeratorEmail");
            HttpContext.Session.Remove("ModeratorRole");
            return RedirectToAction("Login");
        }

        public IActionResult SeedModerator()
        {
            if (!_context.ModeratorSettings.Any(m => m.Email == "artisanvalley.moderator@gmail.com"))
                _context.ModeratorSettings.Add(new ModeratorSetting
                {
                    Email = "artisanvalley.moderator@gmail.com",
                    Password = "Moderator@2026"
                });

            if (!_context.ModeratorSettings.Any(m => m.Email == "artisanvalley.store@gmail.com"))
                _context.ModeratorSettings.Add(new ModeratorSetting
                {
                    Email = "artisanvalley.store@gmail.com",
                    Password = "ArtisanValley@2026"
                });

            _context.SaveChanges();
            return Content("Seeded!");
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
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch { }
        }
        [HttpGet]
        public IActionResult PaymentApprovals()
        {
            if (HttpContext.Session.GetString("ModeratorEmail") == null)
                return RedirectToAction("Login", "Moderator");

            var pending = _context.PaymentSubmissions
                .Where(p => p.Status == "Pending")
                .OrderBy(p => p.SubmittedAt)
                .ToList();

            return View(pending);
        }

        [HttpPost]
        public IActionResult ApprovePayment([FromBody] PaymentActionModel model)
        {
            var moderatorEmail = HttpContext.Session.GetString("ModeratorEmail");
            if (moderatorEmail == null)
                return Json(new { success = false, error = "Unauthorized" });

            var submission = _context.PaymentSubmissions.Find(model.Id);
            if (submission == null || submission.Status != "Pending")
                return Json(new { success = false, error = "Submission not found or already reviewed." });

            submission.Status = "Approved";
            submission.ReviewedAt = DateTime.Now;
            submission.ReviewedBy = moderatorEmail;

            using var con = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
        UPDATE SellerShops 
        SET IsPaid = 1, 
            SubscriptionEnd = DATEADD(MONTH, 1, CASE WHEN SubscriptionEnd < GETDATE() THEN GETDATE() ELSE SubscriptionEnd END)
        WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", submission.SellerEmail);
            cmd.ExecuteNonQuery();

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult RejectPayment([FromBody] PaymentActionModel model)
        {
            var moderatorEmail = HttpContext.Session.GetString("ModeratorEmail");
            if (moderatorEmail == null)
                return Json(new { success = false, error = "Unauthorized" });

            var submission = _context.PaymentSubmissions.Find(model.Id);
            if (submission == null || submission.Status != "Pending")
                return Json(new { success = false, error = "Submission not found or already reviewed." });

            submission.Status = "Rejected";
            submission.RejectionReason = string.IsNullOrWhiteSpace(model.Reason) ? "No reason provided" : model.Reason;
            submission.ReviewedAt = DateTime.Now;
            submission.ReviewedBy = moderatorEmail;

            _context.SaveChanges();
            return Json(new { success = true });
        }

        public class PaymentActionModel
        {
            public int Id { get; set; }
            public string? Reason { get; set; }
        }


    }
}