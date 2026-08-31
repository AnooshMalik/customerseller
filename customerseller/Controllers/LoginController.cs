using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;



namespace customerseller.Controllers
{
    public class LoginController : Controller
    {
        
        private static List<UserData> RegisteredUsers = new List<UserData>
        {
            new UserData { Email = "artisanvalley.store@gamil.com", Password = "Admin@123" }
        };

       
        [HttpGet]
        public IActionResult Login() { return View(); }

        [HttpGet]
        public IActionResult Signup() { return View(); }

        [HttpGet]
        public IActionResult ForgotPassword() { return View(); }

        [HttpPost]
        public IActionResult Signup(string email, string password)
        {
            
            var hasMinimum8Chars = new Regex(@".{8,}");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]+");

            if (string.IsNullOrEmpty(password) || !hasMinimum8Chars.IsMatch(password) || !hasSymbols.IsMatch(password))
            {
                ViewBag.Error = "Signup Failed: Password must be 8+ characters with at least 1 symbol.";
                return View();
            }

           
            if (RegisteredUsers.Any(u => u.Email == email))
            {
                ViewBag.Error = "This email is already registered!";
                return View();
            }

           
            var token = Guid.NewGuid().ToString();
            RegisteredUsers.Add(new UserData
            {
                Email = email,
                Password = password,
                IsVerified = false,
                VerificationToken = token
            });

           
            SendVerificationEmail(email, token);

            ViewBag.Message = "Signup successful! Please check your email to verify your account before logging in.";
            return View();
        }
       
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
           
            if (email == "artisanvalley.store@gamil.com" && password == "Admin@123")
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "Admin");
                return RedirectToAction("Dashboard", "Admin"); 
            }
           
            var user = RegisteredUsers.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null && !user.IsVerified)
            {
                ViewBag.Error = "Please verify your email before logging in. Check your inbox for the verification link.";
                return View();
            }

            if (user != null)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Seller");
                HttpContext.Session.SetString("UserName", email);
                return RedirectToAction("Dashboard", "Dashboard");
            }

            ViewBag.Error = "Invalid email or password. Please try again.";
            return View();
        }


        private void SendVerificationEmail(string toEmail, string token)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Verify Your Email - Artisan Valley";

            var verifyLink = $"https://localhost:5001/Login/VerifyEmail?token={token}";

            message.Body = new TextPart("html")
            {
                Text = $"<h3>Welcome to Artisan Valley!</h3>" +
                       $"<p>Please click the link below to verify your email:</p>" +
                       $"<a href='{verifyLink}'>Verify My Email</a>"
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("artisanvalley.store@gmail.com", "xonaxiahgjobxnjf");   // ✅ YAHAN CHANGE KARNA THA
                client.Send(message);
                client.Disconnect(true);
            }
        }
       
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var userExists = RegisteredUsers.Any(u => u.Email == email);

            if (userExists)
            {
                ViewBag.Message = "A reset link has been sent to your registered email address.";
                return View();
            }
            else
            {
                ViewBag.Error = "This email is not registered with Artisan Valley.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult VerifyEmail(string token)
        {
            var user = RegisteredUsers.FirstOrDefault(u => u.VerificationToken == token);

            if (user == null)
            {
                ViewBag.Error = "Invalid or expired verification link.";
                return View();
            }

            user.IsVerified = true;
            user.VerificationToken = null;
            ViewBag.Message = "Your email has been verified! You can now login.";
            return View();
        }

       
        public IActionResult Logout()
        {
           
            return View();
        }
    }

    public class UserData
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public bool IsVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
    }
}