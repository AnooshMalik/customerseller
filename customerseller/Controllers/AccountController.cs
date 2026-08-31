using BCrypt.Net;
using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace customerseller.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private CookieOptions GetCookieOptions() => new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddDays(30),
            HttpOnly = true,
            IsEssential = true
        };

        [HttpPost]
        public IActionResult Login(string email, string password, string returnUrl, string source)
        {
            bool isSellerPage = source == "sellerpage";

            if (!IsValidEmail(email))
            {
                TempData["LoginError"] = "Please enter a valid email address.";
                if (isSellerPage) return RedirectToAction("Login", new { returnUrl });
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                TempData["LoginError"] = "Password must be at least 8 characters.";
                if (isSellerPage) return RedirectToAction("Login", new { returnUrl });
                return RedirectToAction("Index", "Home");
            }

            var foundUser = _context.Users.FirstOrDefault(u =>
                u.Email.Trim().ToLower() == email.Trim().ToLower());

            if (foundUser == null)
            {
                TempData["LoginError"] = "No account found. Please register first.";
                if (isSellerPage) return RedirectToAction("Login", new { returnUrl });
                return RedirectToAction("Index", "Home", new { returnUrl });
            }
            if (foundUser.IsBlocked)
            {
                if (foundUser.BlockExpiry.HasValue && foundUser.BlockExpiry.Value <= DateTime.Now)
                {
                    foundUser.IsBlocked = false;
                    foundUser.BlockExpiry = null;
                    _context.SaveChanges();
                }
                else if (!foundUser.BlockExpiry.HasValue)
                {
                    TempData["LoginError"] = "Your account has been permanently blocked. Please contact support.";
                    if (isSellerPage) return RedirectToAction("Login", new { returnUrl });
                    return RedirectToAction("Index", "Home", new { returnUrl });
                }
               
            }
            if (BCrypt.Net.BCrypt.Verify(password, foundUser.Password))
            {

                HttpContext.Session.Clear();
                if (foundUser.IsBlocked && foundUser.BlockExpiry.HasValue)
                {
                    HttpContext.Session.SetString("TempBlocked", "true");
                }

                HttpContext.Session.SetString("UserEmail", foundUser.Email);
                HttpContext.Session.SetString("UserName", foundUser.FirstName);
                HttpContext.Session.SetString("UserRole", foundUser.Role ?? "Customer");

                Response.Cookies.Append("UserEmail", foundUser.Email, GetCookieOptions());
                Response.Cookies.Append("UserName", foundUser.FirstName, GetCookieOptions());
                Response.Cookies.Append("UserRole", foundUser.Role ?? "Customer", GetCookieOptions());

                bool isSellerAccount = foundUser.IsSeller || foundUser.Role == "Seller";

                if (isSellerAccount)
                {
                    HttpContext.Session.SetString("UserRole", "Seller");
                    Response.Cookies.Append("UserRole", "Seller", GetCookieOptions());
                    return RedirectToAction("Dashboard", "Dashboard");
                }

                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            TempData["LoginError"] = "Incorrect password.";
            if (isSellerPage) return RedirectToAction("Login", new { returnUrl });
            return RedirectToAction("Index", "Home", new { returnUrl });
        }
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            var sessionEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(sessionEmail))
            {
                var cookieEmail = Request.Cookies["UserEmail"];
                if (!string.IsNullOrEmpty(cookieEmail))
                {
                    var user = _context.Users.FirstOrDefault(u =>
                        u.Email.ToLower() == cookieEmail.ToLower());

                    if (user != null)
                    {
                        HttpContext.Session.SetString("UserEmail", user.Email);
                        HttpContext.Session.SetString("UserName", user.FirstName);
                        HttpContext.Session.SetString("UserRole", user.Role ?? "Customer");
                        sessionEmail = user.Email;
                    }
                }
            }

            if (!string.IsNullOrEmpty(sessionEmail))
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (role == "Seller")
                    return RedirectToAction("Dashboard", "Dashboard");

                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Register(string firstName, string lastName, string email, string password, string returnUrl)
        {
            firstName = firstName?.Trim();
            lastName = lastName?.Trim();
            if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2)
            {
                TempData["RegisterError"] = "Please enter a valid first name.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2)
            {
                TempData["RegisterError"] = "Please enter a valid last name.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            if (!firstName.All(char.IsLetter) || !lastName.All(char.IsLetter))
            {
                TempData["RegisterError"] = "Name must contain letters only.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            if (!IsValidEmail(email))
            {
                TempData["RegisterError"] = "Please enter a valid email address.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            var allowedDomains = new List<string> {
        "gmail.com", "yahoo.com", "hotmail.com",
        "outlook.com", "live.com", "icloud.com",
        "mail.com", "protonmail.com"
    };

            var emailDomain = email.Split('@').Last().ToLower();
            if (!allowedDomains.Contains(emailDomain))
            {
                TempData["RegisterError"] = "Please use a valid email.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                TempData["RegisterError"] = "Password must be at least 8 characters.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email.ToLower()))
            {
                TempData["RegisterError"] = "This email is already registered.";
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            var existingPending = _context.PendingRegistrations.FirstOrDefault(p => p.Email.ToLower() == email.ToLower());
            if (existingPending != null)
                _context.PendingRegistrations.Remove(existingPending);

            var token = Guid.NewGuid().ToString();

            _context.PendingRegistrations.Add(new PendingRegistration
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Token = token
            });
            _context.SaveChanges();
            string verifyLink = string.IsNullOrEmpty(returnUrl)
                ? $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail?token={token}"
                : $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail?token={token}&returnUrl={Uri.EscapeDataString(returnUrl)}";

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", email));
                message.Subject = "Verify Your Email - Artisan Valley";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"<h2>Welcome to Artisan Valley!</h2>
<p>Please click the link below to verify your email address:</p>
<a href='{verifyLink}'>Verify My Email</a>"
                };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                TempData["RegisterError"] = "Registered, but failed to send verification email: " + ex.Message;
                return RedirectToAction("Index", "Home", new { returnUrl });
            }

            TempData["RegisterSuccess"] = "Please check your email to verify your account.";
            return RedirectToAction("Index", "Home", new { returnUrl });
        }

        [HttpGet]
        public IActionResult VerifyEmail(string token)
        {
            var pending = _context.PendingRegistrations.FirstOrDefault(p => p.Token == token);

            if (pending == null)
            {
                TempData["LoginError"] = "Invalid or expired verification link.";
                return RedirectToAction("Index", "Home");
            }

            if (_context.Users.Any(u => u.Email.ToLower() == pending.Email.ToLower()))
            {
                _context.PendingRegistrations.Remove(pending);
                _context.SaveChanges();
                TempData["LoginError"] = "This email is already registered. Please login.";
                return RedirectToAction("Index", "Home");
            }

            var newUser = new User
            {
                FirstName = pending.FirstName,
                LastName = pending.LastName,
                Email = pending.Email,
                Password = pending.PasswordHash,
                Role = "Customer"
            };
            _context.Users.Add(newUser);
            _context.PendingRegistrations.Remove(pending);
            _context.SaveChanges();

            HttpContext.Session.Clear();
            HttpContext.Session.SetString("UserEmail", newUser.Email);
            HttpContext.Session.SetString("UserName", newUser.FirstName);
            HttpContext.Session.SetString("UserRole", "Customer");

            Response.Cookies.Append("UserEmail", newUser.Email, GetCookieOptions());
            Response.Cookies.Append("UserName", newUser.FirstName, GetCookieOptions());
            Response.Cookies.Append("UserRole", "Customer", GetCookieOptions());

            TempData["VerifySuccess"] = "Email verified successfully! You're now logged in.";
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserEmail");
            Response.Cookies.Delete("UserName");
            Response.Cookies.Delete("UserRole");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult SellerTerms()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AcceptTerms()
        {
            return RedirectToAction("ShopSetup", "Account");
        }

        
        [HttpGet]
        public IActionResult ShopSetup()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("SellerTerms", "Account");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT IsApproved, AdminStatus FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", email);

            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                bool isApproved = reader["IsApproved"] != DBNull.Value && (bool)reader["IsApproved"];
                string adminStatus = reader["AdminStatus"]?.ToString() ?? "Pending";

               
                if (isApproved && adminStatus == "Approved")
                    return RedirectToAction("Dashboard", "Seller");

                
                if (adminStatus == "Rejected")
                {
                    ViewBag.HasShop = false;
                    ViewBag.RejectedShop = true;
                    return View();
                }

                ViewBag.HasShop = true;
                ViewBag.IsApproved = false;
                return View();
            }
            ViewBag.HasShop = false;
            ViewBag.IsApproved = false;
            return View();
        }

        [HttpPost]
        public IActionResult ShopSetup(string OwnerName, string CNIC, string ShopName, List<string> Categories, string SellerAddress, string SellerArea, string SellerPhone)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
            {
                TempData["SetupError"] = "Session expired. Please login again.";
                return RedirectToAction("SellerTerms", "Account");
            }
            var cnicPattern = new Regex(@"^\d{5}-\d{7}-\d{1}$");
            if (!cnicPattern.IsMatch(CNIC ?? ""))
            {
                TempData["SetupError"] = "CNIC format must be: 12345-1234567-1";
                return RedirectToAction("ShopSetup", "Account");
            }

            
            var prefixDigits = int.Parse(CNIC.Substring(0, 2));
            if (prefixDigits < 1 || prefixDigits > 61)
            {
                TempData["SetupError"] = "Invalid CNIC — starting digits do not match a valid Pakistani region code.";
                return RedirectToAction("ShopSetup", "Account");
            }

            
            var cleanPhone = (SellerPhone ?? "").Replace("-", "");
            if (cleanPhone.Length != 11 || !cleanPhone.StartsWith("03"))
            {
                TempData["SetupError"] = "Invalid Number";
                return RedirectToAction("ShopSetup", "Account");
            }

            

            if (Categories == null || Categories.Count == 0)
            {
                TempData["SetupError"] = "Please select at least one category.";
                return RedirectToAction("ShopSetup", "Account");
            }

            var subCategories = string.Join(",", Categories);

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var nameCheck = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT COUNT(*) FROM SellerShops WHERE ShopName = @shop", con);
            nameCheck.Parameters.AddWithValue("@shop", ShopName);
            int nameCount = (int)nameCheck.ExecuteScalar();

            if (nameCount > 0)
            {
                TempData["SetupError"] = "This shop name is already taken. Please choose a different name.";

              
                return RedirectToAction("ShopSetup", "Account");
            }

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
    INSERT INTO SellerShops 
    (SellerEmail, OwnerName, ShopName, CNIC, SubCategories, TermsAccepted, 
     SubscriptionStart, SubscriptionEnd, IsPaid, IsApproved,
     SellerAddress, SellerArea, SellerPhone)
    VALUES 
    (@email, @owner, @shop, @cnic, @cats, 1, @start, @end, 0, 0,
     @address, @area, @phone)", con);

            cmd.Parameters.AddWithValue("@address", SellerAddress ?? "");
            cmd.Parameters.AddWithValue("@area", SellerArea ?? "");
            cmd.Parameters.AddWithValue("@phone", SellerPhone ?? "");

            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@owner", OwnerName);
            cmd.Parameters.AddWithValue("@shop", ShopName);
            cmd.Parameters.AddWithValue("@cnic", CNIC);
            cmd.Parameters.AddWithValue("@cats", subCategories);
            cmd.Parameters.AddWithValue("@start", DateTime.Now);
            cmd.Parameters.AddWithValue("@end", DateTime.Now.AddMonths(1));

            cmd.ExecuteNonQuery();

            HttpContext.Session.SetString("ShopName", ShopName);
            Response.Cookies.Append("ShopName", ShopName, GetCookieOptions());

            TempData["SetupSuccess"] = "Shop request submitted successfully!";
            return RedirectToAction("AddProduct", "Products");
        }

        [HttpPost]
        public IActionResult ResetPassword(string email)
        {
            if (!IsValidEmail(email))
            {
                TempData["ResetMessage"] = "Please enter a valid email address.";
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                TempData["ResetMessage"] = "No account found with this email.";
                return RedirectToAction("Index", "Home");
            }

            string token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddHours(1);



            _context.SaveChanges();

            string resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPasswordConfirm?token={token}";

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", email));
                message.Subject = "Password Reset Request";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"<h2>Password Reset</h2>
                    <p>Click the link below to reset your password:</p>
                    <a href='{resetLink}'>Reset Password</a>
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
                TempData["ResetMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }

            TempData["ResetMessage"] = "Reset link sent!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirm(string token)
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.ResetToken == token && u.ResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                TempData["ResetMessage"] = "Invalid or expired reset link.";
                return RedirectToAction("Index", "Home");
            }

            return View((object)token);
        }

        [HttpPost]
        public IActionResult ResetPasswordConfirm(string token, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.ResetToken == token && u.ResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                TempData["ResetMessage"] = "Invalid or expired reset link.";
                return RedirectToAction("Index", "Home");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            _context.SaveChanges();

            TempData["LoginError"] = "Password reset successful! Please login.";
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult SellerRegister()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SellerRegister(string firstName, string lastName, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2)
            {
                firstName = firstName?.Trim();
                lastName = lastName?.Trim();
                TempData["RegisterError"] = "Please enter a valid first name.";
                return RedirectToAction("SellerRegister");
            }
            var existingUserCheck = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (existingUserCheck != null && existingUserCheck.IsSeller)
            {
                TempData["RegisterError"] = "This email is already registered as a Seller.";
                return RedirectToAction("SellerRegister");
            }
            if (!IsValidEmail(email))
            {
                TempData["RegisterError"] = "Please enter a valid email address.";
                return RedirectToAction("SellerRegister");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                TempData["RegisterError"] = "Password must be at least 8 characters.";
                return RedirectToAction("SellerRegister");
            }

           

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("SellerOtp", otp);
            HttpContext.Session.SetString("SellerOtpExpiry", DateTime.Now.AddMinutes(5).ToString());
            HttpContext.Session.SetString("SellerRegEmail", email);
            HttpContext.Session.SetString("SellerRegFirstName", firstName);
            HttpContext.Session.SetString("SellerRegLastName", lastName);
            HttpContext.Session.SetString("SellerRegPassword", password);

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", email));
                message.Subject = "Your OTP - Artisan Valley Seller Registration";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"<h2>Welcome to Artisan Valley!</h2>
                    <p>Your OTP for seller registration:</p>
                    <h1 style='color:#a64d79; font-size:36px;'>{otp}</h1>
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
                TempData["RegisterError"] = "Email error: " + ex.Message;
                return RedirectToAction("SellerRegister");
            }

            return RedirectToAction("SellerVerifyOtp");
        }

        [HttpPost]
        public IActionResult ResendSellerRegOtp()
        {
            var email = HttpContext.Session.GetString("SellerRegEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Session expired. Please register again." });

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("SellerOtp", otp);
            HttpContext.Session.SetString("SellerOtpExpiry", DateTime.Now.AddMinutes(5).ToString());

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", email));
                message.Subject = "Your OTP (Resent) - Artisan Valley Seller Registration";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"<h2>Welcome to Artisan Valley!</h2>
                    <p>Your new OTP for seller registration:</p>
                    <h1 style='color:#a64d79; font-size:36px;'>{otp}</h1>
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
        public IActionResult SellerVerifyOtp()
        {
            if (HttpContext.Session.GetString("SellerRegEmail") == null)
                return RedirectToAction("SellerRegister");
            return View();
        }

        [HttpPost]
        public IActionResult SellerVerifyOtp(string otp)
        {
            var savedOtp = HttpContext.Session.GetString("SellerOtp");
            var expiryStr = HttpContext.Session.GetString("SellerOtpExpiry");
            var email = HttpContext.Session.GetString("SellerRegEmail");
            var firstName = HttpContext.Session.GetString("SellerRegFirstName");
            var lastName = HttpContext.Session.GetString("SellerRegLastName");
            var password = HttpContext.Session.GetString("SellerRegPassword");

            if (savedOtp == null || expiryStr == null)
            {
                TempData["OtpError"] = "Session expired. Please register again.";
                return RedirectToAction("SellerRegister");
            }

            if (DateTime.Now > DateTime.Parse(expiryStr))
            {
                TempData["OtpError"] = "OTP expired. Please register again.";
                return RedirectToAction("SellerRegister");
            }

            if (otp != savedOtp)
            {
                TempData["OtpError"] = "Invalid OTP. Please try again.";
                return View();
            }
            var existingUser = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (existingUser != null)
            {
                existingUser.IsSeller = true;
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(password);
            }
            else
            {
                _context.Users.Add(new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = "Customer",
                    IsSeller = true
                });
            }
            _context.SaveChanges();

            HttpContext.Session.SetString("IsSeller", "True");
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserName", firstName);
            HttpContext.Session.SetString("UserRole", "Seller");

            Response.Cookies.Append("UserEmail", email, GetCookieOptions());
            Response.Cookies.Append("UserName", firstName, GetCookieOptions());
            Response.Cookies.Append("UserRole", "Seller", GetCookieOptions());

            HttpContext.Session.Remove("SellerOtp");
            HttpContext.Session.Remove("SellerOtpExpiry");
            HttpContext.Session.Remove("SellerRegEmail");
            HttpContext.Session.Remove("SellerRegFirstName");
            HttpContext.Session.Remove("SellerRegLastName");
            HttpContext.Session.Remove("SellerRegPassword");

            return RedirectToAction("SellerTerms", "Account");
        }

        public IActionResult GetUserRole()
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "Guest";
            return Json(new { role = role });
        }
        [HttpGet]
        public IActionResult LoginVerifyOtp()
        {
            if (HttpContext.Session.GetString("SellerLoginEmail") == null)
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public IActionResult LoginVerifyOtp(string otp)
        {
            var savedOtp = HttpContext.Session.GetString("SellerLoginOtp");
            var expiryStr = HttpContext.Session.GetString("SellerLoginOtpExpiry");
            var email = HttpContext.Session.GetString("SellerLoginEmail");
            var returnUrl = HttpContext.Session.GetString("SellerLoginReturnUrl");

            if (savedOtp == null || expiryStr == null || email == null)
            {
                TempData["LoginError"] = "Session expired. Please login again.";
                return RedirectToAction("Login");
            }

            if (DateTime.Now > DateTime.Parse(expiryStr))
            {
                TempData["LoginError"] = "OTP expired. Please login again.";
                return RedirectToAction("Login");
            }

            if (otp != savedOtp)
            {
                TempData["OtpError"] = "Invalid OTP. Please try again.";
                return View();
            }

            var foundUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (foundUser == null)
            {
                TempData["LoginError"] = "Account not found.";
                return RedirectToAction("Login");
            }
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("UserEmail", foundUser.Email);
            HttpContext.Session.SetString("UserName", foundUser.FirstName);
            HttpContext.Session.SetString("UserRole", foundUser.Role ?? "Seller");

            Response.Cookies.Append("UserEmail", foundUser.Email, GetCookieOptions());
            Response.Cookies.Append("UserName", foundUser.FirstName, GetCookieOptions());
            Response.Cookies.Append("UserRole", foundUser.Role ?? "Seller", GetCookieOptions());

            HttpContext.Session.Remove("SellerLoginOtp");
            HttpContext.Session.Remove("SellerLoginOtpExpiry");
            HttpContext.Session.Remove("SellerLoginEmail");
            HttpContext.Session.Remove("SellerLoginReturnUrl");

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard", "Dashboard");
        }

        [HttpPost]
        public IActionResult ResendLoginOtp()
        {
            var email = HttpContext.Session.GetString("SellerLoginEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("SellerLoginOtp", otp);
            HttpContext.Session.SetString("SellerLoginOtpExpiry", DateTime.Now.AddMinutes(5).ToString());

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", email));
                message.Subject = "Your Login OTP (Resent) - Artisan Valley";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"<h2>Seller Login OTP</h2>
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

        [HttpPost]
        public IActionResult ResetPasswordAjax(string email)
        {
            if (!IsValidEmail(email))
                return Json(new { success = false, message = "Please enter a valid email address." });

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
                return Json(new { success = false, message = "No account found with this email." });

            string token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddHours(1);
            _context.SaveChanges();

            string resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPasswordConfirm?token={token}";

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MimeKit.MailboxAddress("", email));
                message.Subject = "Password Reset Request";
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = $@"<h2>Password Reset</h2>
                    <p>Click the link below to reset your password:</p>
                    <a href='{resetLink}'>Reset Password</a>
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
                return Json(new { success = false, message = "Email error: " + ex.Message });
            }

            return Json(new { success = true, message = "Reset link sent to your email!" });
        }
    }
}