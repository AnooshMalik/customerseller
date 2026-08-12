using customerseller.Models; // Naye models ka namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using ClosedXML.Excel;
using customerseller.Helpers;

namespace customerseller.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ProductsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ===== FABRIC SUB-CATEGORIES =====
        public IActionResult FabricCushionCovers() => CategoryView("CushionCovers", "Cushion Covers");
        public IActionResult FabricKurtas() => CategoryView("PrintedKurtas", "Printed Kurtas");
        public IActionResult FabricDupattas() => CategoryView("TieDyeDupattas", "Tie-Dye Dupattas");
        public IActionResult FabricToteBags() => CategoryView("FabricToteBags", "Tote Bags");

        // ===== WOOL SUB-CATEGORIES =====
        public IActionResult WoolSweaters() => CategoryView("Sweaters", "Sweaters");
        public IActionResult WoolCrochetBags() => CategoryView("CrochetBags", "Crochet Bags");
        public IActionResult WoolStorageBaskets() => CategoryView("StorageBaskets", "Storage Baskets");
        public IActionResult WoolBeanies() => CategoryView("Beanies", "Beanies");

        // ===== LEATHER SUB-CATEGORIES =====
        public IActionResult LeatherHandbags() => CategoryView("LeatherHandbags", "Handbags");
        public IActionResult LeatherWallets() => CategoryView("Wallets", "Wallets");
        public IActionResult LeatherBelts() => CategoryView("Belts", "Belts");
        public IActionResult LeatherKeychains() => CategoryView("LeatherKeychains", "Keychains");
        public IActionResult LeatherJournals() => CategoryView("Journals", "Journals");

        // ===== WOOD SUB-CATEGORIES =====
        public IActionResult WoodPhotoFrames() => CategoryView("PhotoFrames", "Photo Frames");
        public IActionResult WoodNecklaces() => CategoryView("WoodNecklaces", "Necklaces");
        public IActionResult WoodToys() => CategoryView("WoodToys", "Toys");
        public IActionResult WoodServingTrays() => CategoryView("ServingTrays", "Serving Trays");
        public IActionResult WoodShowpieces() => CategoryView("WoodShowpieces", "Showpieces");
        public IActionResult WoodKeychains() => CategoryView("WoodKeychains", "Keychains");

        // ===== CLAY SUB-CATEGORIES =====
        public IActionResult ClayFlowerPots() => CategoryView("FlowerPots", "Flower Pots");
        public IActionResult ClayCoffeeMugs() => CategoryView("CoffeeMugs", "Coffee Mugs");
        public IActionResult ClayVases() => CategoryView("ClayVases", "Vases");
        public IActionResult ClayEarrings() => CategoryView("ClayEarrings", "Earrings");
        public IActionResult ClayFigurines() => CategoryView("ClayFigurines", "Figurines");

        // ===== JEWELS SUB-CATEGORIES =====
        public IActionResult JewelsNecklaces() => CategoryView("Necklaces", "Necklaces");
        public IActionResult JewelsBracelets() => CategoryView("Bracelets", "Bracelets");
        public IActionResult JewelsRings() => CategoryView("Rings", "Rings");
        public IActionResult JewelsBangles() => CategoryView("Bangles", "Bangles");
        public IActionResult JewelsEarrings() => CategoryView("JewelEarrings", "Earrings");

        // ===== PAPER SUB-CATEGORIES =====
        public IActionResult PaperGreetingCards() => CategoryView("GreetingCards", "Greeting Cards");
        public IActionResult PaperGiftBoxes() => CategoryView("GiftBoxes", "Gift Boxes");
        public IActionResult PaperFlowers() => CategoryView("PaperFlowers", "Paper Flowers");
        public IActionResult PaperShowpieces() => CategoryView("PaperShowpieces", "Showpieces");
        public IActionResult PaperScrapbooks() => CategoryView("Scrapbooks", "Scrapbooks");

        // ===== JUTE SUB-CATEGORIES =====
        public IActionResult JuteBags() => CategoryView("JuteBags", "Bags");
        public IActionResult JuteBaskets() => CategoryView("Baskets", "Baskets");
        public IActionResult JuteTrays() => CategoryView("Trays", "Trays");
        public IActionResult JuteDriedBouquets() => CategoryView("DriedBouquets", "Dried Bouquets");
        public IActionResult JutePaintings() => CategoryView("Paintings", "Paintings");

        // ===== RESIN SUB-CATEGORIES =====
        public IActionResult ResinCoasters() => CategoryView("Coasters", "Coasters");
        public IActionResult ResinEarrings() => CategoryView("ResinEarrings", "Earrings");
        public IActionResult ResinKeychains() => CategoryView("ResinKeychains", "Keychains");
        public IActionResult ResinWallArt() => CategoryView("WallArt", "Wall Art");
        public IActionResult ResinServingTrays() => CategoryView("ResinServingTrays", "Serving Trays");

        // ===== METAL SUB-CATEGORIES =====
        public IActionResult MetalEarrings() => CategoryView("MetalEarrings", "Earrings");
        public IActionResult MetalVases() => CategoryView("MetalVases", "Vases");
        public IActionResult MetalFigurines() => CategoryView("MetalFigurines", "Figurines");
        public IActionResult MetalWallArt() => CategoryView("MetalWallArt", "Wall Art");
        public IActionResult MetalCandleHolders() => CategoryView("CandleHolders", "Candle Holders");
        public IActionResult ClayPottery()
        {
            var products = _context.Products
                .Where(p => (p.Category == "FlowerPots" ||
                             p.Category == "CoffeeMugs" ||
                             p.Category == "ClayVases" ||
                             p.Category == "ClayEarrings" ||
                             p.Category == "ClayFigurines")
                       && p.VideoStatus == "Approved"
                       && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Clay & Pottery";
            return View("CategoryPage", products);
        }
        public IActionResult FabricTextile()
        {
            var products = _context.Products
                .Where(p => (p.Category == "CushionCovers" || p.Category == "PrintedKurtas" ||
                             p.Category == "TieDyeDupattas" || p.Category == "FabricToteBags")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Fabric & Textile";
            return View("CategoryPage", products);
        }

        // ===== WOOL & YARN =====
        public IActionResult WoolYarn()
        {
            var products = _context.Products
                .Where(p => (p.Category == "Sweaters" || p.Category == "CrochetBags" ||
                             p.Category == "StorageBaskets" || p.Category == "Beanies")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Wool & Yarn";
            return View("CategoryPage", products);
        }

        // ===== LEATHER =====
        public IActionResult Leather()
        {
            var products = _context.Products
                .Where(p => (p.Category == "LeatherHandbags" || p.Category == "Wallets" ||
                             p.Category == "Belts" || p.Category == "LeatherKeychains" ||
                             p.Category == "Journals")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Leather";
            return View("CategoryPage", products);
        }

        // ===== WOOD =====
        public IActionResult Wood()
        {
            var products = _context.Products
                .Where(p => (p.Category == "PhotoFrames" || p.Category == "WoodNecklaces" ||
                             p.Category == "WoodToys" || p.Category == "ServingTrays" ||
                             p.Category == "WoodShowpieces" || p.Category == "WoodKeychains")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Wood";
            return View("CategoryPage", products);
        }

        // ===== JEWELS =====
        public IActionResult Jewels()
        {
            var products = _context.Products
                .Where(p => (p.Category == "Necklaces" || p.Category == "Bracelets" ||
                             p.Category == "Rings" || p.Category == "Bangles" ||
                             p.Category == "JewelEarrings")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Jewels";
            return View("CategoryPage", products);
        }

        // ===== PAPER & CARDS =====
        public IActionResult PaperCards()
        {
            var products = _context.Products
                .Where(p => (p.Category == "GreetingCards" || p.Category == "GiftBoxes" ||
                             p.Category == "PaperFlowers" || p.Category == "PaperShowpieces" ||
                             p.Category == "Scrapbooks")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Paper & Cards";
            return View("CategoryPage", products);
        }

        // ===== JUTE & NATURAL =====
        public IActionResult JuteNatural()
        {
            var products = _context.Products
                .Where(p => (p.Category == "JuteBags" || p.Category == "Baskets" ||
                             p.Category == "Trays" || p.Category == "DriedBouquets" ||
                             p.Category == "Paintings")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Jute & Natural";
            return View("CategoryPage", products);
        }

        // ===== RESIN & EPOXY =====
        public IActionResult ResinEpoxy()
        {
            var products = _context.Products
                .Where(p => (p.Category == "Coasters" || p.Category == "ResinEarrings" ||
                             p.Category == "ResinKeychains" || p.Category == "WallArt" ||
                             p.Category == "ResinServingTrays")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Resin & Epoxy";
            return View("CategoryPage", products);
        }

        public IActionResult MetalWire()
        {
            var products = _context.Products
                .Where(p => (p.Category == "MetalEarrings" || p.Category == "Vases" || p.Category == "MetalVases" ||
                             p.Category == "MetalFigurines" || p.Category == "MetalWallArt" ||
                             p.Category == "CandleHolders")
                       && p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();
            ViewBag.CategoryTitle = "Metal & Wire";
            return View("CategoryPage", products);
        }
        private IActionResult CategoryView(string category, string title)
        {
            var products = _context.Products
                .Where(p => p.Category == category
                       && p.VideoStatus == "Approved"
                       && p.IsAdminApproved == true) // ✅ Admin approved bhi check karo
                .ToList();
            ViewBag.CategoryTitle = title;
            return View("CategoryPage", products);
        }

        public IActionResult AllProducts()
        {
            var products = _context.Products
     .Where(p => p.VideoStatus == "Approved"
            && p.IsAdminApproved == true)
     .ToList();
            ViewBag.CategoryTitle = "All Products";
            return View("CategoryPage", products);
        }

        public IActionResult Details(string id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            var reviewCtrl = new customerseller.Controllers.seller.ReviewController(_config);
            ViewBag.Reviews = reviewCtrl.GetByProduct(id);

            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == product.SellerEmail);
            ViewBag.ShopName = shop?.ShopName ?? product.SellerName ?? "Artisan Valley Seller";

            

            return View("Details", product);
        }

        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ViewBag.CategoryTitle = "Search Results";
                return View("CategoryPage", new List<Product>());
            }

            query = query.ToLower().Trim();

            var categoryMap = new Dictionary<string, string>
    {
        // Fabric & Textile
        { "fabric", "FabricTextile" }, { "textile", "FabricTextile" },
        { "cushion", "FabricCushionCovers" }, { "cushion cover", "FabricCushionCovers" },
        { "kurta", "FabricKurtas" }, { "kurtas", "FabricKurtas" },
        { "dupatta", "FabricDupattas" }, { "dupattas", "FabricDupattas" },
        { "tote", "FabricToteBags" }, { "tote bag", "FabricToteBags" },
        // Wool & Yarn
        { "wool", "WoolYarn" }, { "yarn", "WoolYarn" },
        { "sweater", "WoolSweaters" }, { "sweaters", "WoolSweaters" },
        { "crochet", "WoolCrochetBags" }, { "crochet bag", "WoolCrochetBags" },
        { "storage basket", "WoolStorageBaskets" }, { "storage", "WoolStorageBaskets" },
        { "beanie", "WoolBeanies" }, { "beanies", "WoolBeanies" },
        // Leather
        { "leather", "Leather" },
        { "handbag", "LeatherHandbags" }, { "handbags", "LeatherHandbags" },
        { "wallet", "LeatherWallets" }, { "wallets", "LeatherWallets" },
        { "belt", "LeatherBelts" }, { "belts", "LeatherBelts" },
        { "keychain", "LeatherKeychains" }, { "keychains", "LeatherKeychains" },
        { "journal", "LeatherJournals" }, { "journals", "LeatherJournals" },
        // Wood
        { "wood", "Wood" },
        { "photo frame", "WoodPhotoFrames" }, { "frame", "WoodPhotoFrames" },
        { "toy", "WoodToys" }, { "toys", "WoodToys" },
        { "serving tray", "WoodServingTrays" }, { "tray", "WoodServingTrays" },
        { "showpiece", "WoodShowpieces" }, { "showpieces", "WoodShowpieces" },
        // Clay & Pottery
        { "clay", "ClayPottery" }, { "pottery", "ClayPottery" },
        { "flower pot", "ClayFlowerPots" }, { "pot", "ClayFlowerPots" },
        { "coffee mug", "ClayCoffeeMugs" }, { "mug", "ClayCoffeeMugs" },
        { "vase", "ClayVases" }, { "vases", "ClayVases" },
        { "figurine", "ClayFigurines" }, { "figurines", "ClayFigurines" },
        // Jewels
        { "jewel", "Jewels" }, { "jewelry", "Jewels" }, { "jewels", "Jewels" },
        { "necklace", "JewelsNecklaces" }, { "necklaces", "JewelsNecklaces" },
        { "bracelet", "JewelsBracelets" }, { "bracelets", "JewelsBracelets" },
        { "ring", "JewelsRings" }, { "rings", "JewelsRings" },
        { "bangle", "JewelsBangles" }, { "bangles", "JewelsBangles" },
        { "earring", "JewelsEarrings" }, { "earrings", "JewelsEarrings" },
        // Paper & Cards
        { "paper", "PaperCards" },
        { "greeting card", "PaperGreetingCards" }, { "card", "PaperGreetingCards" },
        { "gift box", "PaperGiftBoxes" }, { "gift", "PaperGiftBoxes" },
        { "paper flower", "PaperFlowers" },
        { "scrapbook", "PaperScrapbooks" },
        // Jute & Natural
        { "jute", "JuteNatural" }, { "natural", "JuteNatural" },
        { "bag", "JuteBags" }, { "bags", "JuteBags" },
        { "basket", "JuteBaskets" }, { "baskets", "JuteBaskets" },
        { "bouquet", "JuteDriedBouquets" },
        { "painting", "JutePaintings" }, { "paintings", "JutePaintings" },
        // Resin & Epoxy
        { "resin", "ResinEpoxy" }, { "epoxy", "ResinEpoxy" },
        { "coaster", "ResinCoasters" }, { "coasters", "ResinCoasters" },
        { "wall art", "ResinWallArt" }, { "wall hanging", "ResinWallArt" },
        // Metal & Wire
        { "metal", "MetalWire" }, { "wire", "MetalWire" },
        { "candle holder", "MetalCandleHolders" }, { "candle", "MetalCandleHolders" },

        { "miti", "ClayPottery" }, { "mitti", "ClayPottery" },
{ "bartan", "ClayPottery" }, { "bartan set", "ClayPottery" },
{ "kapra", "FabricTextile" }, { "kapda", "FabricTextile" },
{ "chandi", "Jewels" }, { "sona", "Jewels" },
{ "lakri", "Wood" }, { "lakdi", "Wood" },
    };

            // Step 1: Exact match
            if (categoryMap.ContainsKey(query))
                return RedirectToAction(categoryMap[query]);

            // Step 2: Partial match
            foreach (var key in categoryMap.Keys)
            {
                if (query.Contains(key) || key.Contains(query))
                    return RedirectToAction(categoryMap[key]);
            }

            var stopWords = new HashSet<string> { "ka", "ki", "ke", "ko", "se", "mein", "hai", "wala", "wali", "aur", "the", "and", "for", "with", "a", "an", "of" };
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !stopWords.Contains(w))
                .ToArray();

            if (words.Length == 0)
            {
                ViewBag.NoResults = true;
                ViewBag.SearchQuery = query;
                ViewBag.CategoryTitle = $"Search Results for \"{query}\"";
                return View("CategoryPage", new List<Product>());
            }
            var allProducts = _context.Products
                .Where(p => p.VideoStatus == "Approved" && p.IsAdminApproved == true)
                .ToList();

            var products = allProducts
                .Where(p => words.Any(w =>
                    p.Title.ToLower().Contains(w) ||
                    (p.Description != null && p.Description.ToLower().Contains(w)) ||
                    p.Category.ToLower().Contains(w)))
                .ToList();

            if (!products.Any())
            {
                ViewBag.NoResults = true;
                ViewBag.SearchQuery = query;
            }

            ViewBag.CategoryTitle = $"Search Results for \"{query}\"";
            return View("CategoryPage", products);
        }
        public IActionResult Filter()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        public IActionResult SellerProduct()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            // Products fetch karo
            var products = _context.Products
                .Where(p => p.SellerEmail == email)
                .ToList();

            // Seller ki saari subcategories bhi bhejo
            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategories FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
            var result = cmd.ExecuteScalar()?.ToString() ?? "";

            ViewBag.AllSubCategories = result.Split(',',
                StringSplitOptions.RemoveEmptyEntries).ToList();

            return View("~/Views/seller/Product/SellerProduct.cshtml", products);
        }

        public IActionResult AddProduct()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT IsApproved FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
            var result = cmd.ExecuteScalar();

            bool isApproved = result != null && result != DBNull.Value && (bool)result;
            ViewBag.ShopPendingApproval = !isApproved;

            return View("~/Views/seller/Product/AddProduct.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile ImageFile, IFormFile VideoFile)
        {
            // Seller info session se lo
            product.SellerEmail = HttpContext.Session.GetString("UserEmail");
            product.SellerName = HttpContext.Session.GetString("UserName");
            product.VideoStatus = "Pending";
            product.IsAdminApproved = false;
            product.Id = Guid.NewGuid().ToString();

            // Image save karo
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Unique filename banao - spaces remove karo
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", "images", fileName);

                // Folder exist karo
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images"));

                using var imageStream = new FileStream(imagePath, FileMode.Create);
                await ImageFile.CopyToAsync(imageStream);
                product.ImageUrl = "/images/" + fileName;
            }

            // Video save karo
            if (VideoFile != null && VideoFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(VideoFile.FileName);
                var videoPath = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", "videos", fileName);

                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos"));

                using var videoStream = new FileStream(videoPath, FileMode.Create);
                await VideoFile.CopyToAsync(videoStream);
                product.VideoUrl = "/videos/" + fileName;

                // SubCategoryVideos mein bhi save karo
                using var con2 = new Microsoft.Data.SqlClient.SqlConnection(
                    _context.Database.GetConnectionString());
                con2.Open();

                var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM SubCategoryVideos WHERE SellerEmail = @email AND SubCategory = @sub", con2);
                checkCmd.Parameters.AddWithValue("@email", product.SellerEmail);
                checkCmd.Parameters.AddWithValue("@sub", product.Category);
                int exists = (int)checkCmd.ExecuteScalar();

                if (exists == 0)
                {
                    var insertCmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "INSERT INTO SubCategoryVideos (SellerEmail, SubCategory, VideoUrl, VideoStatus) VALUES (@email, @sub, @url, 'Pending')", con2);
                    insertCmd.Parameters.AddWithValue("@email", product.SellerEmail);
                    insertCmd.Parameters.AddWithValue("@sub", product.Category);
                    insertCmd.Parameters.AddWithValue("@url", product.VideoUrl);
                    insertCmd.ExecuteNonQuery();
                }
            }
            // Duplicate check: same seller, same title, price — pichle 30 second ke andar
            var recentDuplicate = _context.Products.FirstOrDefault(p =>
                p.SellerEmail == product.SellerEmail &&
                p.Title == product.Title &&
                p.Price == product.Price);

            if (recentDuplicate != null)
            {
                TempData["Error"] = "This product already exists.";
                return RedirectToAction("SellerProduct");
            }

            _context.Products.Add(product);
            _context.SaveChanges();

            _context.Products.Add(product);
            _context.SaveChanges();
            if (Request.Headers["X-Add-More"] == "true")
                return Ok();

            return RedirectToAction("SellerProduct");



        }

        // Check karo subcategory ki video hai ya nahi
        public IActionResult CheckSubCategoryVideo(string subCategory)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT VideoStatus FROM SubCategoryVideos WHERE SellerEmail = @email AND SubCategory = @sub", con);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@sub", subCategory);

            con.Open();
            var result = cmd.ExecuteScalar();

            if (result != null)
                return Json(new { hasVideo = true, status = result.ToString() });

            return Json(new { hasVideo = false });
        }

        public IActionResult MyProductStatus()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                @"SELECT Id, Title, Category, Price, ImageUrl, 
                 VideoStatus, IsAdminApproved, AdminRejectionReason 
          FROM Products 
          WHERE SellerEmail = @email 
          ORDER BY Id DESC", con);
            cmd.Parameters.AddWithValue("@email", email);

            con.Open();
            var reader = cmd.ExecuteReader();
            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader["Id"],
                    title = reader["Title"],
                    category = reader["Category"],
                    price = reader["Price"],
                    imageUrl = reader["ImageUrl"],
                    videoStatus = reader["VideoStatus"],
                    isAdminApproved = (bool)reader["IsAdminApproved"],
                    adminRejectionReason = reader["AdminRejectionReason"] == DBNull.Value
                                           ? "" : reader["AdminRejectionReason"].ToString()
                });
            }

            return Json(list);
        }

        public IActionResult GetSellerCategories()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { categories = new List<string>() });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategories FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", email);

            var result = cmd.ExecuteScalar()?.ToString() ?? "";
            var categories = result.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            return Json(new { categories });
        }
        

        

        [HttpGet]
        public IActionResult EditProduct(string id)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var product = _context.Products
                .FirstOrDefault(p => p.Id == id && p.SellerEmail == email);
            if (product == null) return NotFound();

            // Seller ki categories fetch karo
            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategories FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", email);
            var result = cmd.ExecuteScalar()?.ToString() ?? "";
            ViewBag.SellerCategories = result.Split(',',
                StringSplitOptions.RemoveEmptyEntries).ToList();

            return View("~/Views/seller/Product/EditProduct.cshtml", product);
        }

        [HttpPost]
        public IActionResult DeleteProduct([FromBody] DeleteRequest request)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            var product = _context.Products
                .FirstOrDefault(p => p.Id == request.Id && p.SellerEmail == email);

            if (product == null)
            {
                return Json(new { success = false });
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        public class DeleteRequest
        {
            public string Id { get; set; }
        }


        public IActionResult GetProductJson(string id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return Json(new { error = "Not found" });

            return Json(new
            {
                id = product.Id,
                title = product.Title,
                price = product.Price,
                imageUrl = product.ImageUrl
            });
        }

        [HttpGet]
        public IActionResult UploadSubCategoryVideo(string subcategory)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Home");

            ViewBag.SubCategory = subcategory;
            return View("~/Views/seller/Product/UploadSubCategoryVideo.cshtml");
        }


        [HttpPost]
        public async Task<IActionResult> UploadSubCategoryVideo(string subcategory, IFormFile videoFile)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (videoFile == null || videoFile.Length == 0)
            {
                TempData["Error"] = "Please select a video file.";
                return RedirectToAction("UploadSubCategoryVideo", new { subcategory = subcategory });
            }

            // Video save karo
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
            var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos", fileName);
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos"));

            using (var stream = new FileStream(videoPath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            // SubCategoryVideos mein update karo
            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
     "UPDATE SubCategoryVideos SET VideoUrl = @url, VideoStatus = 'Pending', RejectionReason = NULL WHERE SellerEmail = @email AND SubCategory = @sub", con);
            cmd.Parameters.AddWithValue("@url", "/videos/" + fileName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@sub", subcategory);
            cmd.ExecuteNonQuery();

            // Products table bhi reset karo
            var resetProducts = new Microsoft.Data.SqlClient.SqlCommand(
                @"UPDATE Products 
      SET VideoStatus = 'Pending', 
          IsAdminApproved = 0,
          AdminRejectionReason = NULL
      WHERE SellerEmail = @email AND Category = @sub", con);
            resetProducts.Parameters.AddWithValue("@email", email);
            resetProducts.Parameters.AddWithValue("@sub", subcategory);
            resetProducts.ExecuteNonQuery();

            TempData["Success"] = "Video re-uploaded! Awaiting moderator review.";
            return RedirectToAction("Dashboard", "Dashboard");
        }
       

        [HttpPost]
        public async Task<IActionResult> EditProduct(string id, [FromForm] Product model, IFormFile newImage)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var product = _context.Products.FirstOrDefault(p => p.Id == id && p.SellerEmail == email);

            if (product == null)
                return RedirectToAction("SellerProduct");

            // Product details update karo
            product.Title = model.Title;
            product.Description = model.Description;
            product.Price = model.Price;
            product.Category = model.Category;
            product.Stock = model.Stock;

            // Naya image agar upload hua to use karo
            if (newImage != null && newImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newImage.FileName);
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images"));

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await newImage.CopyToAsync(stream);
                }

                product.ImageUrl = "/images/" + fileName;
            }

            // ✅ Admin approval reset karo
            product.IsAdminApproved = false;
            product.AdminRejectionReason = null;

            _context.Products.Update(product);
            _context.SaveChanges();

            TempData["Success"] = "Product updated! Awaiting admin review.";
            return RedirectToAction("Dashboard", "Dashboard");
        }
        private static string NormalizeSubCategory(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return System.Text.RegularExpressions.Regex
                .Replace(input.Trim(), @"\s+", "")
                .ToLowerInvariant();
        }
        [HttpGet]
        public IActionResult BulkUpload()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            // Seller ki allowed subcategories (jo usne shop banate waqt select ki thi)
            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategories FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
            var result = cmd.ExecuteScalar()?.ToString() ?? "";
            var sellerSubCats = result.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Main Category -> seller ki approved SubCategories (sirf non-empty groups)
            var grouped = CategoryMap.MainToSub
                .Select(kv => new
                {
                    MainCategory = kv.Key,
                    SubCategories = kv.Value.Where(sub => sellerSubCats.Contains(sub)).ToList()
                })
                .Where(g => g.SubCategories.Any())
                .ToDictionary(g => g.MainCategory, g => g.SubCategories);

            ViewBag.GroupedSubCategories = grouped;
            ViewBag.SellerSubCategories = sellerSubCats;

            return View("~/Views/seller/Product/BulkUpload.cshtml");
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategories FROM SellerShops WHERE SellerEmail = @email", con);
            cmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
            var result = cmd.ExecuteScalar()?.ToString() ?? "";
            var sellerSubCats = result.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!sellerSubCats.Any())
                return NotFound("You have no approved sub-categories yet.");

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Products");

            ws.Cell(1, 1).Value = "Title";
            ws.Cell(1, 2).Value = "Price";
            ws.Cell(1, 3).Value = "Stock";
            ws.Cell(1, 4).Value = "SubCategory";
            ws.Cell(1, 5).Value = "Color";
            ws.Cell(1, 6).Value = "Sizes";
            ws.Cell(1, 7).Value = "Description";
            ws.Cell(1, 8).Value = "Image (insert the picture into this column's cell)";

            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#a64d79");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            ws.Column(1).Width = 25;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 10;
            ws.Column(4).Width = 20;
            ws.Column(5).Width = 12;
            ws.Column(6).Width = 15;
            ws.Column(7).Width = 35;
            ws.Column(8).Width = 20;

            ws.Cell(2, 1).Value = "Example: Product Name";
            ws.Cell(2, 2).Value = 1500;
            ws.Cell(2, 3).Value = 10;
            ws.Cell(2, 4).Value = sellerSubCats.First();
            ws.Cell(2, 5).Value = "blue";
            ws.Cell(2, 6).Value = "S, M, L";
            ws.Cell(2, 7).Value = "This row is just an example, delete it";

            // Dusri sheet: dropdown ka source (valid subcategories)
            var ws2 = wb.Worksheets.Add("ValidSubCategories");
            ws2.Cell(1, 1).Value = "SubCategory Code";
            ws2.Cell(1, 1).Style.Font.Bold = true;
            int r = 2;
            foreach (var sub in sellerSubCats)
            {
                ws2.Cell(r, 1).Value = sub;
                r++;
            }
            ws2.Column(1).Width = 30;
            ws2.Visibility = ClosedXML.Excel.XLWorksheetVisibility.VeryHidden;

            // Column D (SubCategory) pe dropdown lagao
            var dataRange = ws.Range("D2:D500");
            var validationSourceRange = $"ValidSubCategories!$A$2:$A${sellerSubCats.Count + 1}";
            dataRange.SetDataValidation().List(validationSourceRange, true);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BulkUploadTemplate.xlsx");
        }

        // ---------------------------------------------------------------------------
        // 3) EXCEL + VIDEOS UPLOAD KARO — actual processing
        // ---------------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> BulkUpload(IFormFile excelFile, List<IFormFile> videoFiles)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var name = HttpContext.Session.GetString("UserName");

            if (excelFile == null || excelFile.Length == 0)
                return Json(new { success = false, message = "Excel file select karein." });

            // Seller ki allowed subcategories
            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();
            var subCmd = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT SubCategories FROM SellerShops WHERE SellerEmail = @email", con);
            subCmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
            var subResult = subCmd.ExecuteScalar()?.ToString() ?? "";
            var sellerSubCats = subResult.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var videoBySubCat = new Dictionary<string, IFormFile>();
            if (videoFiles != null)
            {
                foreach (var vf in videoFiles)
                {
                    var key = NormalizeSubCategory(Path.GetFileNameWithoutExtension(vf.FileName));
                    if (!videoBySubCat.ContainsKey(key))
                        videoBySubCat[key] = vf;
                }
            }

            var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            var videosDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos");
            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(videosDir);

            var errors = new List<string>();
            var toInsert = new List<Product>();
            var subCatsHandledThisBatch = new HashSet<string>(); // taake ek subcategory ka video sirf 1 dafa insert ho

            using var excelStream = excelFile.OpenReadStream();
            using var wb = new ClosedXML.Excel.XLWorkbook(excelStream);
            var ws = wb.Worksheet(1);

            var picturesByRow = new Dictionary<int, byte[]>();
            foreach (var pic in ws.Pictures)
            {
                int row = pic.TopLeftCell.Address.RowNumber;
                if (!picturesByRow.ContainsKey(row))
                {
                    using var ms = new MemoryStream();
                    pic.ImageStream.Position = 0;
                    pic.ImageStream.CopyTo(ms);
                    picturesByRow[row] = ms.ToArray();
                }
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var title = ws.Cell(row, 1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(title)) continue; // khaali row skip

                if (title.StartsWith("Example:")) continue; // template ki example row skip

                var priceCell = ws.Cell(row, 2);
                var stockCell = ws.Cell(row, 3);
                var subCategory = ws.Cell(row, 4).GetString().Trim();
                var color = ws.Cell(row, 5).GetString().Trim();
                var sizes = ws.Cell(row, 6).GetString().Trim();
                var description = ws.Cell(row, 7).GetString().Trim();

                // ---- Validation ----
                if (!decimal.TryParse(priceCell.GetString(), out var price) || price <= 0)
                {
                    errors.Add($"Row {row}: Price is invalid.");
                    continue;
                }
                if (!int.TryParse(stockCell.GetString(), out var stock) || stock <= 0)
                {
                    errors.Add($"Row {row}: Stock is invalid.");
                    continue;
                }
                var matchedSubCat = sellerSubCats.FirstOrDefault(s =>
      NormalizeSubCategory(s) == NormalizeSubCategory(subCategory));

                if (matchedSubCat == null)
                {
                    errors.Add($"Row {row}: '{subCategory}' is not a valid SubCategory for your shop.");
                    continue;
                }
                subCategory = matchedSubCat; // ✅ ab aage jo bhi ho, correct/canonical naam use hoga
                if (!picturesByRow.ContainsKey(row))
                {
                    errors.Add($"Row {row}: No picture was inserted in the Image column.");
                    continue;
                }

                // ---- Video check: kya is subcategory ka video already hai? ----
                var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM SubCategoryVideos WHERE SellerEmail = @email AND SubCategory = @sub", con);
                checkCmd.Parameters.AddWithValue("@email", email);
                checkCmd.Parameters.AddWithValue("@sub", subCategory);
                bool videoAlreadyExists = (int)checkCmd.ExecuteScalar() > 0;

                string videoUrlForThisProduct = null;

                if (!videoAlreadyExists && !subCatsHandledThisBatch.Contains(subCategory))
                {
                    if (!videoBySubCat.TryGetValue(NormalizeSubCategory(subCategory), out var matchedVideo))
                    {
                        errors.Add($"Row {row}: No video file was uploaded for '{subCategory}' (file name must be '{subCategory}.mp4').");
                        continue;
                    }

                    // Video save karo
                    var vFileName = Guid.NewGuid().ToString() + Path.GetExtension(matchedVideo.FileName);
                    var vPath = Path.Combine(videosDir, vFileName);
                    using (var vStream = new FileStream(vPath, FileMode.Create))
                    {
                        await matchedVideo.CopyToAsync(vStream);
                    }
                    videoUrlForThisProduct = "/videos/" + vFileName;

                    var insertVideoCmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "INSERT INTO SubCategoryVideos (SellerEmail, SubCategory, VideoUrl, VideoStatus) VALUES (@email, @sub, @url, 'Pending')", con);
                    insertVideoCmd.Parameters.AddWithValue("@email", email);
                    insertVideoCmd.Parameters.AddWithValue("@sub", subCategory);
                    insertVideoCmd.Parameters.AddWithValue("@url", videoUrlForThisProduct);
                    insertVideoCmd.ExecuteNonQuery();

                    subCatsHandledThisBatch.Add(subCategory);
                }

                var imgBytes = picturesByRow[row];
                var imgFileName = Guid.NewGuid().ToString() + ".png";
                var imgPath = Path.Combine(imagesDir, imgFileName);
                await System.IO.File.WriteAllBytesAsync(imgPath, imgBytes);
                var product = new Product
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    Price = price,
                    Stock = stock.ToString(),
                    Category = subCategory,
                    Color = string.IsNullOrWhiteSpace(color) ? null : color,
                    Sizes = string.IsNullOrWhiteSpace(sizes) ? null : sizes,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    ImageUrl = "/images/" + imgFileName,
                    VideoUrl = videoUrlForThisProduct,
                    VideoStatus = "Pending",
                    IsAdminApproved = false,
                    SellerEmail = email,
                    SellerName = name,
                    Status = "Pending"
                };

                toInsert.Add(product);
            }

            if (toInsert.Any())
            {
                _context.Products.AddRange(toInsert);
                _context.SaveChanges();
            }

            return Json(new
            {
                success = true,
                inserted = toInsert.Count,
                errorCount = errors.Count,
                errors
            });
        }
    }
}