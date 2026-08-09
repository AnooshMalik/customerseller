using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace customerseller.Controllers
{
    public class LoginController : Controller
    {
        // Temporary Database
        private static List<UserData> RegisteredUsers = new List<UserData>
        {
            new UserData { Email = "artisanvalley.store@gamil.com", Password = "Admin@123" }
        };

        // --- GET PAGES ---
        [HttpGet]
        public IActionResult Login() { return View(); }

        [HttpGet]
        public IActionResult Signup() { return View(); }

        [HttpGet]
        public IActionResult ForgotPassword() { return View(); }

        // --- SIGNUP PROCESS ---
        [HttpPost]
        public IActionResult Signup(string email, string password)
        {
            // 1. Password Format Check (8+ chars & 1 Symbol)
            var hasMinimum8Chars = new Regex(@".{8,}");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]+");

            if (string.IsNullOrEmpty(password) || !hasMinimum8Chars.IsMatch(password) || !hasSymbols.IsMatch(password))
            {
                ViewBag.Error = "Signup Failed: Password must be 8+ characters with at least 1 symbol.";
                return View();
            }

            // 2. Check if user already exists
            if (RegisteredUsers.Any(u => u.Email == email))
            {
                ViewBag.Error = "This email is already registered!";
                return View();
            }

            // 3. Add new user and redirect to Login
            RegisteredUsers.Add(new UserData { Email = email, Password = password });
            return RedirectToAction("Login");
        }

        // --- LOGIN PROCESS ---
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Admin check - hardcoded
            if (email == "artisanvalley.store@gamil.com" && password == "Admin@123")
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "Admin");
                return RedirectToAction("Dashboard", "Admin"); // Admin dashboard
            }

            // Seller check - registered users se
            var user = RegisteredUsers.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserRole", "Seller");
                HttpContext.Session.SetString("UserName", email);
                return RedirectToAction("Dashboard", "Dashboard"); // Seller dashboard
            }

            ViewBag.Error = "Invalid email or password. Please try again.";
            return View();
        }

        // --- FORGOT PASSWORD PROCESS ---
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

        // --- LOGOUT PROCESS (SELLERMVC SE SHIFT KIYA GAYA) ---
        public IActionResult Logout()
        {
            // Agar session clear karna ho toh baad mein yahan line add kar lenge
            return View();
        }
    }

    // Model class ko controller file ke end mein (namespace ke andar) adjust kar diya
    public class UserData
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}