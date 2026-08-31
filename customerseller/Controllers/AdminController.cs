using customerseller.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace customerseller.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
       

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public IActionResult Login()
        {
            
            if (HttpContext.Session.GetString("AdminEmail") != null)
                return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var admin = _context.AdminSettings.FirstOrDefault(a =>
                a.Email.ToLower() == email.ToLower());

            if (admin != null && admin.Password == password)
            {
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("AdminOtp", otp);
                HttpContext.Session.SetString("AdminOtpExpiry",
                    DateTime.Now.AddMinutes(5).ToString());
                HttpContext.Session.SetString("AdminEmailPending", email);

                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("Artisan Valley",
                        "artisanvalley.store@gmail.com"));
                    message.To.Add(new MailboxAddress("Admin", admin.Email));
                    message.Subject = "Admin Login OTP";
                    message.Body = new TextPart("html")
                    {
                        Text = $@"<h2>Admin Login OTP</h2>
                <p>Your OTP is: <strong style='font-size:24px;
                color:#a64d79;'>{otp}</strong></p>
                <p>This OTP expires in 5 minutes.</p>"
                    };

                    using var client = new MailKit.Net.Smtp.SmtpClient();
                    client.Connect("smtp.gmail.com", 587,
                        MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate("artisanvalley.store@gmail.com",
                        "esphtmdtnnptlhuo");
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    TempData["AdminError"] = "Email error: " + ex.Message;
                    return View();
                }

                return RedirectToAction("Otp");
            }

            TempData["AdminError"] = "Invalid email or password.";
            return View();
        }
      
        [HttpGet]
        public IActionResult Otp()
        {
            if (HttpContext.Session.GetString("AdminEmailPending") == null)
                return RedirectToAction("Login");
            return View();
        }

        
        [HttpPost]
        public IActionResult Otp(string otp)
        {
            var savedOtp = HttpContext.Session.GetString("AdminOtp");
            var expiryStr = HttpContext.Session.GetString("AdminOtpExpiry");

            if (savedOtp == null || expiryStr == null)
            {
                TempData["OtpError"] = "Session expired. Please login again.";
                return RedirectToAction("Login");
            }

            var expiry = DateTime.Parse(expiryStr);
            if (DateTime.Now > expiry)
            {
                TempData["AdminError"] = "OTP expired. Please login again.";
                return RedirectToAction("Login");
            }

            if (otp == savedOtp)
            {
                HttpContext.Session.Clear();
               
                HttpContext.Session.SetString("AdminEmail", "artisanvalley.store@gmail.com");
                HttpContext.Session.SetString("AdminRole", "Admin");

                
                HttpContext.Session.Remove("AdminOtp");
                HttpContext.Session.Remove("AdminOtpExpiry");
                HttpContext.Session.Remove("AdminEmailPending");

                return RedirectToAction("Dashboard");
            }

            TempData["OtpError"] = "Invalid OTP. Please try again.";
            return View();
        }
       
        [HttpPost]
        public IActionResult ResendOtp()
        {
            var pendingEmail = HttpContext.Session.GetString("AdminEmailPending");
            if (string.IsNullOrEmpty(pendingEmail))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var admin = _context.AdminSettings.FirstOrDefault(a =>
                a.Email.ToLower() == pendingEmail.ToLower());
            if (admin == null)
                return Json(new { success = false, message = "Admin account not found." });

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("AdminOtp", otp);
            HttpContext.Session.SetString("AdminOtpExpiry",
                DateTime.Now.AddMinutes(5).ToString());

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("Admin", admin.Email));
                message.Subject = "Admin Login OTP (Resent)";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Admin Login OTP</h2>
                <p>Your new OTP is: <strong style='font-size:24px; color:#a64d79;'>{otp}</strong></p>
                <p>This OTP expires in 5 minutes.</p>"
                };

                using var client = new MailKit.Net.Smtp.SmtpClient();
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
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var pendingShopsCmd = new Microsoft.Data.SqlClient.SqlCommand(
     @"SELECT COUNT(*) FROM SellerShops 
      WHERE (IsApproved = 0 OR IsApproved IS NULL) 
      AND (AdminStatus = 'Pending' OR AdminStatus IS NULL)", con);
            ViewBag.PendingShopsCount = (int)pendingShopsCmd.ExecuteScalar();

            var pendingProductsCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT COUNT(*) FROM Products WHERE IsAdminApproved = 0 AND VideoStatus = 'Approved'", con);
            ViewBag.PendingProductsCount = (int)pendingProductsCmd.ExecuteScalar();

            var totalOrders = _context.Orders.Count();
            var pendingOrders = _context.Orders.Count(o => o.Status == "Pending" || o.Status == "Placed" || o.Status == "Processing");
            var netRevenue = _context.Orders
                .Where(o => o.Status == "Delivered")
                .Sum(o => (decimal?)o.Total) ?? 0;

            var recentOrders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            var recentSales = recentOrders.Select(o => new customerseller.Models.SaleActivity
            {
                Detail = $"Order #{o.OrderId} — {o.CustomerName ?? (o.FirstName + " " + o.LastName)}",
                Amount = $"Rs. {o.Total:N0}"
            }).ToList();

            var model = new customerseller.Models.DashboardViewModel
            {
                SellerName = "Admin",
                ShopName = "Artisan Valley",
                AccountStatus = "",
                NetRevenue = netRevenue,
                RevenueGrowth = 0,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                Rating = 0,
                LiveVisitors = 0,
                RecentSales = recentSales
            };

            return View(model);

          
        }
        public IActionResult GetPendingShops()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
     @"SELECT Id, ShopName, OwnerName, SellerEmail, CNIC 
      FROM SellerShops 
      WHERE (IsApproved = 0 OR IsApproved IS NULL) 
      AND (AdminStatus = 'Pending' OR AdminStatus IS NULL)", con);
            con.Open();
            var reader = cmd.ExecuteReader();
            var shops = new List<object>();

            while (reader.Read())
            {
                shops.Add(new
                {
                    Id = reader["Id"],
                    ShopName = reader["ShopName"],
                    OwnerName = reader["OwnerName"],
                    Email = reader["SellerEmail"],
                    CNIC = reader["CNIC"]
                });
            }

            return Json(shops);
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("AdminEmail");
            HttpContext.Session.Remove("AdminRole");
            return RedirectToAction("Login");
        }

        
        public IActionResult GetPendingProducts()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
        SELECT Id, Title, Category, Price, SellerEmail, SellerName, 
               ImageUrl, VideoUrl, VideoStatus
        FROM Products 
        WHERE IsAdminApproved = 0 AND VideoStatus = 'Approved'", con);

            con.Open();
            var reader = cmd.ExecuteReader();
            var products = new List<object>();

            while (reader.Read())
            {
                products.Add(new
                {
                    Id = reader["Id"],
                    Title = reader["Title"],
                    Category = reader["Category"],
                    Price = reader["Price"],
                    SellerEmail = reader["SellerEmail"],
                    SellerName = reader["SellerName"],
                    ImageUrl = reader["ImageUrl"],
                    VideoUrl = reader["VideoUrl"],
                    VideoStatus = reader["VideoStatus"]
                });
            }

            return Json(products);
        }

       
        [HttpPost]
        public IActionResult ApproveProduct(string id)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE Products SET IsAdminApproved = 1, AdminApprovedAt = @now WHERE Id = @id", con);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", id);

            con.Open();
            cmd.ExecuteNonQuery();

            return Json(new { success = true });
        }

       
        [HttpPost]
        public IActionResult RejectProduct(string id, string reason)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE Products SET IsAdminApproved = 0, AdminRejectionReason = @reason WHERE Id = @id", con);
            cmd.Parameters.AddWithValue("@reason", reason ?? "");
            cmd.Parameters.AddWithValue("@id", id);

            con.Open();
            cmd.ExecuteNonQuery();

            return Json(new { success = true });
        }

        
        public IActionResult GetStats()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var stats = new
            {
                users = _context.Users.Count(u => u.Role == "Buyer"),
                sellers = _context.Users.Count(u => u.Role == "Seller"),
                products = _context.Products.Count(),
                orders = _context.Orders.Count()
            };
            return Json(stats);
        }

        
        public IActionResult GetUsers()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var users = _context.Users
                .Where(u => u.Role == "Buyer")
                .Select(u => new {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role
                }).ToList();
            return Json(users);
        }

       
        public IActionResult GetSellers()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var sellers = _context.Users
                .Where(u => u.Role == "Seller")
                .Select(u => new {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role
                }).ToList();
            return Json(sellers);
        }

        
        public IActionResult GetProducts()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var products = _context.Products
                .Select(p => new {
                    p.Id,
                    p.Title,
                    p.Category,
                    p.Price
                }).ToList();
            return Json(products);
        }

        public IActionResult GetOrders()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var orders = _context.Orders
    .Select(o => new {
        o.OrderId,
        o.CustomerName,
        Total = o.Price,
        Status = o.Status,
        OrderDate = o.OrderDate
    }).ToList();
            return Json(orders);
        }
        [HttpPost]
        public IActionResult BlockUser([FromBody] BlockUserModel model)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var user = _context.Users.Find(model.Id);
            if (user == null) return Json(new { error = "User not found" });

            user.IsBlocked = true;
            user.BlockReason = model.Reason;

            if (model.Days.HasValue && model.Days.Value > 0)
                user.BlockExpiry = DateTime.Now.AddDays(model.Days.Value);
            else
                user.BlockExpiry = null;

            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UnblockUser([FromBody] BlockUserModel model)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var user = _context.Users.Find(model.Id);
            if (user == null) return Json(new { error = "User not found" });

            user.IsBlocked = false;
            user.BlockExpiry = null;
            user.BlockReason = null;
            _context.SaveChanges();
            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult RemoveProduct([FromBody] ProductActionModel model)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            if (string.IsNullOrEmpty(model?.Id))
                return Json(new { error = "Invalid product id" });

            var product = _context.Products.FirstOrDefault(p => p.Id == model.Id);
            if (product == null) return Json(new { error = "Product not found" });

            _context.Products.Remove(product);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var admin = _context.AdminSettings.FirstOrDefault(a => a.Email == email);
            if (admin == null)
            {
                TempData["ForgotError"] = "No admin account found with this email.";
                return View();
            }

            var token = Guid.NewGuid().ToString();
            admin.ResetToken = token;
            admin.ResetTokenExpiry = DateTime.Now.AddHours(1);
            _context.SaveChanges();

            string resetLink = $"{Request.Scheme}://{Request.Host}/Admin/ResetPassword?token={token}";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("Admin", email));
                message.Subject = "Admin Password Reset";
                message.Body = new TextPart("html")
                {
                    Text = $@"<h2>Admin Password Reset Request</h2>
<p>Click the link below to reset your password:</p>
<a href='{resetLink}' style='background:#a64d79; color:#fff; padding:12px 24px; border-radius:8px; text-decoration:none;'>Reset Password</a>
<p>This link expires in 1 hour.</p>"
                };
                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                TempData["ForgotError"] = "Email error: " + ex.Message;
                return View();
            }

            TempData["ForgotSuccess"] = "Reset link sent to your email!";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var admin = _context.AdminSettings.FirstOrDefault(a =>
                a.ResetToken == token && a.ResetTokenExpiry > DateTime.Now);

            if (admin == null)
            {
                TempData["ResetError"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

       

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword)
        {
            var admin = _context.AdminSettings.FirstOrDefault(a =>
                a.ResetToken == token && a.ResetTokenExpiry > DateTime.Now);

            if (admin == null)
            {
                TempData["ResetError"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            admin.Password = newPassword; 
            admin.ResetToken = null;
            admin.ResetTokenExpiry = null;
            _context.SaveChanges();

            TempData["AdminError"] = "Password reset successful! Please login.";
            return RedirectToAction("Login");
        }


       public IActionResult ShopRequests()
{
    if (HttpContext.Session.GetString("AdminEmail") == null)
        return RedirectToAction("Login");

    var shops = _context.SellerShops
        .OrderByDescending(s => s.Id)
        .ToList();

    return View(shops);
}
        [HttpPost]
        public IActionResult AdminApproveShop([FromBody] ShopActionModel model)
        {
            var shop = _context.SellerShops.Find(model.Id);
            if (shop == null) return Json(new { error = "Not found" });

            shop.IsApproved = true;
            shop.AdminStatus = "Approved";
            shop.ApprovedAt = DateTime.Now;
            _context.SaveChanges();
            shop.AdminStatus = "Approved";
            shop.ApprovedAt = DateTime.Now;
            _context.SaveChanges();

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", shop.SellerEmail));
                message.Subject = "Your Shop is Approved! — Artisan Valley";
                message.Body = new TextPart("html")
                {
                    Text = $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
        <h2 style='color:#a64d79;'>Shop Approved! 🎉</h2>
        <p>Your shop <strong>{shop.ShopName}</strong> has been approved.</p>
        <p>Login to your dashboard and start listing products.</p>
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
        public IActionResult AdminRejectShop([FromBody] ShopActionModel model)
        {
            var shop = _context.SellerShops.Find(model.Id);
            if (shop == null) return Json(new { error = "Not found" });

            shop.AdminStatus = "Rejected";
            shop.RejectionReason = model.Reason;
            _context.SaveChanges();

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", shop.SellerEmail));
                message.Subject = "Shop Application Update — Artisan Valley";
                message.Body = new TextPart("html")
                {
                    Text = $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                <h2 style='color:#c62828;'>Shop Application Update</h2>
                <p>Your shop <strong>{shop.ShopName}</strong> was not approved.</p>
                <p><strong>Reason:</strong> {model.Reason}</p>
                <p>You may re-apply after making necessary changes.</p>
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
        public IActionResult AllSellers()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            var sellers = _context.Users
                .Where(u => u.Role == "Seller" || u.IsSeller)
                .ToList();

            var shops = _context.SellerShops.ToList();

            ViewBag.Shops = shops;
            ViewBag.ApprovedShops = shops.Count(s => s.AdminStatus == "Approved");
            ViewBag.PendingShops = shops.Count(s => s.AdminStatus == "Pending" || s.AdminStatus == null);

            return View(sellers);
        }
        public IActionResult VideoReviews()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            var products = _context.Products
                .Where(p => p.VideoUrl != null)
                .OrderByDescending(p => p.ReviewedAt)
                .ToList();

            return View(products);
        }

        public IActionResult Moderators()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");
            return View();
        }

        public IActionResult Products()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            
            var rejectedSubs = _context.SubCategoryVideos
                .Where(v => v.VideoStatus == "Rejected")
                .Select(v => v.SellerEmail + "|" + v.SubCategory)
                .ToHashSet();

            var products = _context.Products
                .OrderByDescending(p => p.ReviewedAt)
                .ToList()
                .Where(p => !rejectedSubs.Contains(p.SellerEmail + "|" + p.Category))
                .ToList();

           
            var subVideos = _context.SubCategoryVideos.ToList();

            var videoMap = subVideos
                .GroupBy(v => v.SellerEmail + "|" + v.SubCategory)
                .ToDictionary(g => g.Key, g => g.First().VideoUrl);

            ViewBag.VideoMap = videoMap;

            ViewBag.Total = products.Count;
            ViewBag.Approved = products.Count(p => p.IsAdminApproved == true);
            ViewBag.Pending = products.Count(p => p.IsAdminApproved == false && p.VideoStatus == "Approved" && p.AdminRejectionReason == null);
            ViewBag.Rejected = products.Count(p => p.IsAdminApproved == false && p.AdminRejectionReason != null);
            return View(products);
        }
        public IActionResult Orders()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            var orders = _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var allProductIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId)).Distinct().ToList();
            var productMap = _context.Products
     .Where(p => allProductIds.Contains(p.Id))
     .ToDictionary(p => p.Id, p => (object)new { p.Category, p.SubCategory });

            ViewBag.ProductMap = productMap;

            ViewBag.Total = orders.Count;
            ViewBag.Pending = orders.Count(o => o.Status == "Pending" || o.Status == "Placed");
            ViewBag.Delivered = orders.Count(o => o.Status == "Delivered");

            return View("AdminOrders", orders);
        }
        public IActionResult Reports()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            ViewBag.TotalRevenue = _context.Orders
      .Where(o => o.Status == "Delivered")
      .Sum(o => (decimal?)o.Total) ?? 0;

            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalSellers = _context.Users.Count(u => u.Role == "Seller");
            ViewBag.TotalCustomers = _context.Users.Count(u => u.Role == "Customer");
            ViewBag.TotalProducts = _context.Products.Count(p => p.IsAdminApproved == true);
            ViewBag.TotalComplaints = _context.Complaints.Count();
            ViewBag.PendingComplaints = _context.Complaints.Count(c => c.Status == "Open" || c.Status == "Escalated");
            ViewBag.PendingShops = _context.SellerShops.Count(s => s.AdminStatus == "Pending" || s.AdminStatus == null);

            var deliveredOrders = _context.Orders.Where(o => o.Status == "Delivered").ToList();
            ViewBag.AvgOrderValue = deliveredOrders.Any() ? deliveredOrders.Average(o => o.Total) : 0;
            ViewBag.TopProducts = _context.Products
                .Where(p => p.IsAdminApproved == true)
                .OrderByDescending(p => p.Stock)
                .Take(5)
                .ToList();

            return View("AdminReports");
        }

      
        
        public IActionResult Complaints()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            var complaints = _context.Complaints
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            ViewBag.Total = complaints.Count;
            ViewBag.Open = complaints.Count(c => c.Status == "Open");
            ViewBag.Forwarded = complaints.Count(c => c.Status == "Forwarded");
            ViewBag.Resolved = complaints.Count(c => c.Status == "Resolved");
            ViewBag.Escalated = complaints.Count(c => c.Status == "Escalated");

            return View(complaints);
        }
        public IActionResult GetOpenComplaints()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var count = _context.Complaints
                .Count(c => c.Status == "Open" || c.Status == "Escalated");

            return Json(new { count = count });
        }

        public IActionResult ShopDetail(int id)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return RedirectToAction("Login");

            var shop = _context.SellerShops.Find(id);
            if (shop == null) return NotFound();

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

           
            var subVideos = _context.SubCategoryVideos
                .Where(v => v.SellerEmail == shop.SellerEmail)
                .ToList();

            ViewBag.SubVideos = subVideos;

           
            var productsCmd = new Microsoft.Data.SqlClient.SqlCommand(
                @"SELECT p.* FROM Products p
          INNER JOIN SubCategoryVideos sv ON p.SellerEmail = sv.SellerEmail 
                                          AND p.Category = sv.SubCategory
          WHERE p.SellerEmail = @email 
          AND sv.VideoStatus = 'Approved'
          AND p.IsAdminApproved = 0", con);
            productsCmd.Parameters.AddWithValue("@email", shop.SellerEmail);

            var reader = productsCmd.ExecuteReader();
            var products = new List<Product>();

            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = reader["Id"].ToString(),
                    Title = reader["Title"].ToString(),
                    Category = reader["Category"].ToString(),
                    Price = (decimal)reader["Price"],
                    ImageUrl = reader["ImageUrl"]?.ToString(),
                    VideoUrl = reader["VideoUrl"]?.ToString()
                });
            }
            reader.Close();

            ViewBag.Products = products;
            return View(shop);
        }

        [HttpPost]
        public IActionResult ApproveSubCategory([FromBody] SubCategoryRejectModel model)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var shop = _context.SellerShops.Find(model.ShopId);
            if (shop == null) return Json(new { error = "Shop not found" });

            var video = _context.SubCategoryVideos
                .FirstOrDefault(v => v.SellerEmail == shop.SellerEmail && v.SubCategory == model.SubCategory);

            if (video == null) return Json(new { error = "Subcategory video not found" });

            video.VideoStatus = "Approved";
            _context.SaveChanges();

            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult RejectSubCategory([FromBody] SubCategoryRejectModel model)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null)
                return Json(new { error = "Unauthorized" });

            var shop = _context.SellerShops.Find(model.ShopId);
            if (shop == null) return Json(new { error = "Not found" });

           
            var rejectedList = string.IsNullOrEmpty(shop.RejectedSubCategories)
                ? new List<string>()
                : shop.RejectedSubCategories.Split(',').ToList();

            var reasonList = string.IsNullOrEmpty(shop.SubCategoryRejectionReasons)
                ? new List<string>()
                : shop.SubCategoryRejectionReasons.Split('|').ToList();

            if (!rejectedList.Contains(model.SubCategory))
            {
                rejectedList.Add(model.SubCategory);
                reasonList.Add($"{model.SubCategory}:{model.Reason}");
            }

            shop.RejectedSubCategories = string.Join(",", rejectedList);
            shop.SubCategoryRejectionReasons = string.Join("|", reasonList);
            _context.SaveChanges();

           
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", shop.SellerEmail));
                message.Subject = "Subcategory Rejected — Artisan Valley";
                message.Body = new TextPart("html")
                {
                    Text = $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
                <h2 style='color:#c62828;'>Subcategory Video Rejected</h2>
                <p>Your subcategory <strong>{model.SubCategory}</strong> video has been rejected.</p>
                <p><strong>Reason:</strong> {model.Reason}</p>
                <p>Please login and re-upload the video for this subcategory.</p>
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
    }
}