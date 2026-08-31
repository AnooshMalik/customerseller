using customerseller.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Diagnostics;

namespace customerseller.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration config)
        {
            _logger = logger;
            _context = context;
            _config = config;
        }

        public IActionResult Index()
        {
            var products = _context.Products
                .Where(p => p.IsAdminApproved)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();

            return View(products);
        }

       
        public IActionResult About() => View();
       
        public IActionResult Contact() => View();

        [HttpPost]
        public async Task<IActionResult> Contact(string Name, string Email, string Phone, string Subject, string Message)
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Message))
            {
                TempData["ContactError"] = "Please fill in all required fields.";
                return RedirectToAction("Contact");
            }

           
            using (var con = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString()))
            {
                con.Open();
                var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    @"INSERT INTO ContactMessages (Name, Email, Phone, Subject, Message, SubmittedAt) 
              VALUES (@name, @email, @phone, @subject, @message, @submittedAt)", con);
                cmd.Parameters.AddWithValue("@name", Name);
                cmd.Parameters.AddWithValue("@email", Email);
                cmd.Parameters.AddWithValue("@phone", (object)Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@subject", Subject);
                cmd.Parameters.AddWithValue("@message", Message);
                cmd.Parameters.AddWithValue("@submittedAt", DateTime.Now);
                cmd.ExecuteNonQuery();
            }

           
            try
            {
                var fromEmail = _config["EmailSettings:FromEmail"];
                var appPassword = _config["EmailSettings:AppPassword"];
                var smtpHost = _config["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Artisan Valley Contact Form", fromEmail));
                mimeMessage.To.Add(new MailboxAddress("Artisan Valley", fromEmail));
                mimeMessage.ReplyTo.Add(new MailboxAddress(Name, Email));
                mimeMessage.Subject = $"New Contact Message: {Subject}";
                mimeMessage.Body = new TextPart("plain")
                {
                    Text = $"Name: {Name}\nEmail: {Email}\nPhone: {Phone}\n\nMessage:\n{Message}"
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(fromEmail, appPassword);
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact form email failed to send");
                
            }

            TempData["ContactSuccess"] = "Thank you! Your message has been sent. We'll get back to you soon.";
            return RedirectToAction("Contact");
        }
        public IActionResult Privacy() => View();
        public IActionResult Refund() => View();
        public IActionResult Shipping() => View();
        public IActionResult Story() => View();
        public IActionResult Terms() => View();
        public IActionResult SellerIndex() => View("SellerIndex");
        public IActionResult SellerAbout() => View("SellerAbout");
        public IActionResult SellerPrivacy() => View("SellerPrivacy");
        public IActionResult SellerTerms() => View("SellerTerms");
        public IActionResult Help() => View();
        [HttpPost]
        public IActionResult SubscribeNewsletter(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Please enter a valid email." });

            using (var con = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString()))
            {
                con.Open();

                var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM NewsletterSubscribers WHERE Email = @email", con);
                checkCmd.Parameters.AddWithValue("@email", email);
                int exists = (int)checkCmd.ExecuteScalar();

                if (exists > 0)
                {
                    return Json(new { success = false, message = "You're already subscribed!" });
                }

                var insertCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "INSERT INTO NewsletterSubscribers (Email, SubscribedAt) VALUES (@email, @date)", con);
                insertCmd.Parameters.AddWithValue("@email", email);
                insertCmd.Parameters.AddWithValue("@date", DateTime.Now);
                insertCmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Thank you for subscribing!" });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}