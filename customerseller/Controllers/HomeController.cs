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
            var bestSellerIds = new List<string>
    {
        "3f38c029-bfd0-433c-b92b-24c7f7c38440", 
        "b1b46c0b-bbf5-48e2-b6ef-efbc8d9ed876", 
        "64ed08ff-b538-4214-9fa8-52595d57a0a0", 
        "6cdbd0fe-a742-447b-b39d-be8092e9a03f"  
    };

            var products = _context.Products
                .Where(p => bestSellerIds.Contains(p.Id))
                .ToList();

            
            products = products.OrderBy(p => bestSellerIds.IndexOf(p.Id)).ToList();

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