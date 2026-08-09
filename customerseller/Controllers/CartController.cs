using Microsoft.EntityFrameworkCore;
using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace customerseller.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Checkout()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                TempData["OpenLoginSidebar"] = "true";
                return RedirectToAction("Index", "Home", new { returnUrl = "/Cart/Checkout" });
            }
            return View(new CheckoutViewModel());
        }
        [HttpPost]
        public IActionResult ProcessOrder(CheckoutViewModel model, string cartData)
        {
            if (!ModelState.IsValid)
            {
                ModelState.Clear();
            }

            if (string.IsNullOrEmpty(cartData))
            {
                return Json(new { success = false, message = "Cart data missing." });
            }

            try
            {
                var cartItems = JsonSerializer.Deserialize<List<customerseller.Models.CartItem>>(cartData);
                foreach (var item in cartItems)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        var shop = _context.SellerShops.FirstOrDefault(s =>
     s.SellerEmail.Trim().ToLower() == product.SellerEmail.Trim().ToLower());
                        item.SellerShopName = shop?.ShopName ?? product.SellerName ?? "Artisan Valley Seller";
                    }
                }
                if (cartItems == null || !cartItems.Any())
                {
                    return Json(new { success = false, message = "Your cart is empty." });
                }

                decimal total = cartItems.Sum(item => item.Price * item.Quantity);

                if (decimal.TryParse(Request.Form["deliveryFee"], out decimal dFee))
                    total += dFee;

                int lastNumber = 0;
                var lastOrder = _context.Orders
                    .OrderByDescending(o => o.OrderId)
                    .FirstOrDefault();

                if (lastOrder != null)
                {
                    var parts = lastOrder.OrderId.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int parsed))
                    {
                        lastNumber = parsed;
                    }
                }

                string orderId = $"AV-{(lastNumber + 1):D3}";

                var order = new Order
                {

                    OrderId = orderId,
                    CustomerName = (model.FirstName + " " + model.LastName).Trim(), // YEH ADD KARO
                    Email = HttpContext.Session.GetString("UserEmail") ?? model.Email,
                  
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CustomerPhone = Request.Form["Email"].ToString(),  // ✅ YAHI RAKHNI HAI

                    Address = model.Address,
                    City = model.City ?? "Rawalpindi",
                    Country = model.Country ?? "Pakistan",
                    PaymentMethod = model.PaymentMethod ?? "COD",
                    Total = total,
                    DeliveryFee = dFee,
                    OrderDate = DateTime.Now,
                    Status = "Processing",
                    TrackingId = "AV-TRACK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    Items = cartItems.Select(i => new customerseller.Models.CartItem
                    {
                        ProductId = i.ProductId,
                        Title = i.Title,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        ImageUrl = i.ImageUrl,
                        DisplayId = i.DisplayId,
                        SellerShopName = i.SellerShopName
                    }).ToList(),
                    TrackingTimeline = new List<TrackingStep>
                    {
                        new TrackingStep { Status = "Order Placed", Timestamp = DateTime.Now, Description = "Your order has been received.", Completed = true, Active = false },
                        new TrackingStep { Status = "Processing", Timestamp = DateTime.Now, Description = "We are preparing your items.", Completed = true, Active = true },
                        new TrackingStep { Status = "Shipped", Description = "Your package is on the way.", Completed = false, Active = false },
                        new TrackingStep { Status = "Out for Delivery", Description = "Our rider is nearby.", Completed = false, Active = false },
                        new TrackingStep { Status = "Delivered", Description = "Order has been delivered.", Completed = false, Active = false }
                    }
                };

                _context.Orders.Add(order);

                try
                {
                    _context.SaveChanges();
                }
                catch (Exception dbEx)
                {
                    var inner = dbEx.InnerException?.Message ?? "No inner exception";
                    return Json(new { success = false, message = "DB Error: " + dbEx.Message + " | Inner: " + inner });
                }

                // Email
                try
                {
                    string itemsList = string.Join("", order.Items.Select(i =>
                        $"<tr><td style='padding:8px; border-bottom:1px solid #eee;'>{i.Title}</td>" +
                        $"<td style='padding:8px; border-bottom:1px solid #eee;'>x{i.Quantity}</td>" +
                        $"<td style='padding:8px; border-bottom:1px solid #eee;'>Rs. {(i.Price * i.Quantity):N0}</td></tr>"
                    ));

                    var message = new MimeKit.MimeMessage();
                    message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", ".store@gmail.com"));
                    var userEmail = HttpContext.Session.GetString("UserEmail");
                    message.To.Add(new MimeKit.MailboxAddress("", userEmail));
                    message.Subject = $"Order Confirmed! #{order.OrderId} - Artisan Valley";
                    message.Body = new MimeKit.TextPart("html")
                    {
                        Text = $@"<div style='font-family:sans-serif;'>Order #{order.OrderId} confirmed. Total: Rs. {order.Total:N0}</div>"
                    };

                    using var client = new MailKit.Net.Smtp.SmtpClient();
                    client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(".store@gmail.com", "esphtmdtnnptlhuo");
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception emailEx)
                {
                    // Email fail hone par order cancel nahi hoga
                    Console.WriteLine("Email error: " + emailEx.Message);
                }

                return Json(new { success = true, redirectUrl = "/Cart/OrderConfirmed?id=" + orderId });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                return Json(new { success = false, message = "Error: " + ex.Message + " | Inner: " + inner });
            }
        }


        [HttpGet]
        public IActionResult TrackOrder(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return View("/Views/Cart/TrackOrder.cshtml", null);

           
            string searchId = orderId.Trim().ToUpper();

            var order = _context.Orders
                .Include(o => o.Items)
                .Include(o => o.TrackingTimeline)
                .FirstOrDefault(o => o.OrderId.Trim().ToUpper() == searchId);

            if (order == null)
            {
                TempData["TrackError"] = $"No order found with ID: {orderId}. Please check and try again.";
                return View("/Views/Cart/TrackOrder.cshtml", null);
            }

            return View("/Views/Cart/TrackOrder.cshtml", order);
        }

        [HttpPost]
        public IActionResult CancelOrder(string orderId)
        {
            if (string.IsNullOrEmpty(orderId)) return BadRequest("Order ID is required");

            var order = _context.Orders
                .Include(o => o.TrackingTimeline)
                .FirstOrDefault(o => o.OrderId.ToUpper() == orderId.ToUpper().Trim());

            if (order == null) return NotFound("Order not found");

            if (order.Status == "Shipped" || order.Status == "OutForDelivery" || order.Status == "Delivered")
                return BadRequest("Order cannot be cancelled at this stage.");

            order.Status = "Cancelled";
            order.CancelledDate = DateTime.Now;
            order.TrackingTimeline.Add(new TrackingStep
            {
                OrderId = orderId,
                Status = "Cancelled",
                Timestamp = DateTime.Now,
                Description = "Cancelled by user.",
                Completed = true,
                Active = false
            });

            _context.SaveChanges();
            return RedirectToAction("TrackOrder", new { orderId });
        }
       

        [HttpPost]
        public IActionResult ReturnOrder(string orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order != null)
            {
                order.Status = "Return Requested";
                _context.SaveChanges();
            }
            return RedirectToAction("TrackOrder", new { orderId });
        }
        public IActionResult ShoppingCart() => View();

        [HttpGet]
        public IActionResult OrderConfirmed(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var order = _context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.TrackingTimeline)
                    .AsNoTracking()
                    .FirstOrDefault(o => o.OrderId == id);

                if (order != null)
                {
                    // Null safety
                    order.Items ??= new List<CartItem>();
                    order.TrackingTimeline ??= new List<TrackingStep>();
                    return View(order);
                }
            }

            return View();
        }

        public IActionResult GetDeliveryCharge(string area)
        {
            if (string.IsNullOrEmpty(area))
                return Json(new { charge = 0, zone = "", time = "" });

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

           
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
        SELECT dz.DeliveryCharge, dz.ZoneName, dz.EstimatedTime
        FROM AreaZoneMapping azm
        JOIN DeliveryZones dz ON azm.ZoneId = dz.Id
        WHERE LOWER(azm.AreaName) = LOWER(@area)", con);
            cmd.Parameters.AddWithValue("@area", area.Trim());

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return Json(new
                    {
                        charge = reader["DeliveryCharge"],
                        zone = reader["ZoneName"],
                        time = reader["EstimatedTime"],
                        found = true
                    });
                }
            }

           
            var cmd2 = new Microsoft.Data.SqlClient.SqlCommand(@"
        SELECT TOP 1 dz.DeliveryCharge, dz.ZoneName, dz.EstimatedTime
        FROM AreaZoneMapping azm
        JOIN DeliveryZones dz ON azm.ZoneId = dz.Id
        WHERE SOUNDEX(azm.AreaName) = SOUNDEX(@area)
           OR DIFFERENCE(azm.AreaName, @area) >= 3
        ORDER BY DIFFERENCE(azm.AreaName, @area) DESC", con);
            cmd2.Parameters.AddWithValue("@area", area.Trim());

            using (var reader2 = cmd2.ExecuteReader())
            {
                if (reader2.Read())
                {
                    return Json(new
                    {
                        charge = reader2["DeliveryCharge"],
                        zone = reader2["ZoneName"],
                        time = reader2["EstimatedTime"],
                        found = true
                    });
                }
            }

            return Json(new { charge = 0, zone = "", time = "", found = false });
        }

        public IActionResult GetAreas(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 3)
                return Json(new List<string>());

            using var con = new Microsoft.Data.SqlClient.SqlConnection(
                _context.Database.GetConnectionString());
            con.Open();

          
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
        SELECT TOP 8 AreaName 
        FROM AreaZoneMapping 
        WHERE LOWER(AreaName) LIKE LOWER(@term)
        ORDER BY LEN(AreaName)", con);
            cmd.Parameters.AddWithValue("@term", "%" + term + "%");

            var areas = new List<string>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    areas.Add(reader["AreaName"].ToString()!);
            }

            if (areas.Any())
                return Json(areas);

            var cmd2 = new Microsoft.Data.SqlClient.SqlCommand(@"
        SELECT TOP 5 AreaName 
        FROM AreaZoneMapping 
        WHERE SOUNDEX(AreaName) = SOUNDEX(@term)
           OR DIFFERENCE(AreaName, @term) >= 3", con);
            cmd2.Parameters.AddWithValue("@term", term);

            using (var reader2 = cmd2.ExecuteReader())
            {
                while (reader2.Read())
                    areas.Add(reader2["AreaName"].ToString()!);
            }

            return Json(areas);
        }
        [HttpGet]
        public IActionResult Complaint(string? orderId, string? amount)
        {
            ViewBag.OrderId = orderId ?? "";
            ViewBag.Amount = amount ?? "";
            return View();
        }
    }
}