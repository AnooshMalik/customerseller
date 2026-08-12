using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using customerseller.Models;

namespace customerseller.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}