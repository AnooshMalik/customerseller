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
        private readonly IConfiguration _configuration;

        public CartController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName) ||
                string.IsNullOrWhiteSpace(model.Phone) || string.IsNullOrWhiteSpace(model.Address))
            {
                return Json(new { success = false, message = "Please fill in all required fields (name, phone, address)." });
            }
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                return Json(new { success = false, message = "Please login first." });

            if (HttpContext.Session.GetString("UserRole") == "Seller")
                return Json(new { success = false, message = "Sellers cannot place orders. Please login as a customer." });

            model.FirstName = model.FirstName.Trim();
            model.LastName = model.LastName.Trim();
            model.Address = model.Address.Trim();
            model.Phone = model.Phone.Trim();

            var nameRx = new System.Text.RegularExpressions.Regex(@"^[A-Za-z]+( [A-Za-z]+)*$");
            if (model.FirstName.Length < 2 || model.FirstName.Length > 30 || !nameRx.IsMatch(model.FirstName) ||
                model.LastName.Length < 2 || model.LastName.Length > 30 || !nameRx.IsMatch(model.LastName))
                return Json(new { success = false, message = "Please enter a valid name (letters only)." });

            var addrOk =
    System.Text.RegularExpressions.Regex.IsMatch(model.Address, @"^[A-Za-z0-9\s,.\-/#]{10,200}$") &&
    !System.Text.RegularExpressions.Regex.IsMatch(model.Address, @"[^A-Za-z0-9\s]{2,}") &&
    model.Address.Count(char.IsLetter) >= 6 &&
    model.Address.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3 &&
    !System.Text.RegularExpressions.Regex.IsMatch(model.Address, @"(.)\1{3,}");
            if (!addrOk)
                return Json(new { success = false, message = "Please enter a valid full address." });
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^03\d{2}-?\d{7}$"))
                return Json(new { success = false, message = "Please enter a valid phone number (03XX-XXXXXXX)." });
            if (string.IsNullOrEmpty(cartData))
                return Json(new { success = false, message = "Cart data missing." });

            try
            {
                string selectedArea = Request.Form["selectedArea"].ToString();
                var cartItems = JsonSerializer.Deserialize<List<customerseller.Models.CartItem>>(cartData);
                if (cartItems == null || !cartItems.Any())
                    return Json(new { success = false, message = "Your cart is empty." });

                var sellerByProduct = new Dictionary<string, string>();

                foreach (var item in cartItems)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId
                        && p.VideoStatus == "Approved" && p.IsAdminApproved == true);

                    if (product == null)
                        return Json(new { success = false, message = "One of the items in your cart is no longer available." });

                    int availableStock = int.TryParse(product.Stock, out var stockVal) ? stockVal : 0;
                    if (item.Quantity > availableStock)
                        return Json(new { success = false, message = $"'{product.Title}' only has {availableStock} left in stock." });

                    item.Price = product.Price;
                    item.Title = product.Title;

                    var shop = _context.SellerShops.FirstOrDefault(s =>
                        s.SellerEmail.Trim().ToLower() == product.SellerEmail.Trim().ToLower());
                    item.SellerShopName = shop?.ShopName ?? product.SellerName ?? "Artisan Valley Seller";

                    product.Stock = (availableStock - item.Quantity).ToString();
                    sellerByProduct[item.ProductId] = (product.SellerEmail ?? "unknown").Trim().ToLower();
                }

                
                var groups = cartItems.GroupBy(i => sellerByProduct[i.ProductId]).ToList();

                if (string.IsNullOrWhiteSpace(selectedArea))
                    return Json(new { success = false, message = "Please select your delivery area." });

                var areaList = GetAllAreasWithZones();
                var chosenArea = areaList.FirstOrDefault(a =>
                    a.AreaName.Trim().Equals(selectedArea.Trim(), StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(chosenArea.AreaName))
                    return Json(new { success = false, message = "Invalid delivery area selected." });

                var addrLower = model.Address.ToLowerInvariant();
                var mentionedAreas = areaList
                    .Where(a => a.AreaName.Length >= 4 && addrLower.Contains(a.AreaName.Trim().ToLowerInvariant()))
                    .Select(a => a.AreaName.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                bool chosenMentioned = addrLower.Contains(chosenArea.AreaName.Trim().ToLowerInvariant());
                if (!chosenMentioned)
                {
                    if (mentionedAreas.Count > 0)
                        return Json(new { success = false, message = $"Your address mentions '{mentionedAreas[0]}' but you selected '{chosenArea.AreaName}'. Please select the correct area." });

                    return Json(new { success = false, message = $"Please include the area name '{chosenArea.AreaName}' in your address." });
                }
                var fees = new Dictionary<string, decimal>();
                foreach (var g in groups)
                {
                    decimal fee = CalculateVerifiedDeliveryFee(model.Address, g.ToList(), selectedArea);
                    if (fee <= 0)
                    {
                        return Json(new { success = false, message = "Could not verify delivery area from your address. Please include a valid Rawalpindi area name (e.g. Saddar, DHA, Westridge)." });
                    }
                    fees[g.Key] = fee;
                }

                int lastNumber = 0;
                var lastOrder = _context.Orders.OrderByDescending(o => o.OrderId).FirstOrDefault();
                if (lastOrder != null)
                {
                    var parts = lastOrder.OrderId.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int parsed))
                        lastNumber = parsed;
                }

                string parentId = "AV-GRP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                var createdOrders = new List<Order>();
                string userEmail = HttpContext.Session.GetString("UserEmail");

                foreach (var g in groups)
                {
                    lastNumber++;
                    var items = g.ToList();
                    decimal dFee = fees[g.Key];
                    decimal total = items.Sum(i => i.Price * i.Quantity) + dFee;

                    createdOrders.Add(new Order
                    {
                        OrderId = $"AV-{lastNumber:D3}",
                        ParentOrderId = parentId,
                        CustomerName = (model.FirstName + " " + model.LastName).Trim(),
                        Email = userEmail,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CustomerPhone = model.Phone,
                        Address = model.Address,
                        City = model.City ?? "Rawalpindi",
                        Country = model.Country ?? "Pakistan",
                        PaymentMethod = model.PaymentMethod ?? "COD",
                        Total = total,
                        DeliveryFee = dFee,
                        OrderDate = DateTime.Now,
                        Status = "Processing",
                        TrackingId = "AV-TRACK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        Items = items.Select(i => new customerseller.Models.CartItem
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
                    });
                }

                _context.Orders.AddRange(createdOrders);

                try
                {
                    _context.SaveChanges();
                }
                catch (Exception dbEx)
                {
                    var inner = dbEx.InnerException?.Message ?? "No inner exception";
                    return Json(new { success = false, message = "DB Error: " + dbEx.Message + " | Inner: " + inner });
                }

                // Ek hi email, har seller ke order ke saath
                try
                {
                    string sections = string.Join("", createdOrders.Select(o =>
                    {
                        string rows = string.Join("", o.Items.Select(i =>
                            $"<tr><td style='padding:8px; border-bottom:1px solid #eee; font-size:14px;'>{i.Title}</td>" +
                            $"<td style='padding:8px; border-bottom:1px solid #eee; text-align:center;'>x{i.Quantity}</td>" +
                            $"<td style='padding:8px; border-bottom:1px solid #eee; text-align:right;'>PKR {(i.Price * i.Quantity):N0}</td></tr>"));
                        string shop = o.Items.FirstOrDefault()?.SellerShopName ?? "Seller";
                        return $@"
                        <div style='background:#fdf0f5; border-radius:8px; padding:14px 18px; margin-bottom:16px;'>
                            <p style='margin:0; font-size:13px; color:#a64d79; font-weight:700;'>{shop}</p>
                            <p style='margin:2px 0; font-size:14px;'>Order #{o.OrderId} | Tracking: <strong style='color:#a64d79;'>{o.TrackingId}</strong></p>
                            <table style='width:100%; border-collapse:collapse; margin-top:8px;'>{rows}</table>
                            <p style='text-align:right; margin:8px 0 0; font-weight:700;'>Total: PKR {o.Total:N0} (delivery PKR {o.DeliveryFee:N0} included)</p>
                        </div>";
                    }));

                    decimal grand = createdOrders.Sum(o => o.Total);
                    string emailBody = $@"
                    <div style='font-family:Arial, sans-serif; max-width:600px; margin:0 auto; background:#fdf6f9; padding:30px 20px;'>
                        <div style='background:#fff; border-radius:12px; overflow:hidden;'>
                            <div style='background:#a64d79; padding:30px 25px; text-align:center;'>
                                <h1 style='color:#fff; margin:0; font-size:22px;'>Artisan Valley</h1>
                            </div>
                            <div style='padding:30px 25px;'>
                                <h2 style='margin:0 0 6px;'>Thank you, {model.FirstName}!</h2>
                                <p style='color:#666; font-size:14px;'>Your items come from different sellers, so each seller has its own order and tracking ID.</p>
                                {sections}
                                <p style='text-align:right; font-size:16px; font-weight:700;'>Grand Total: PKR {grand:N0}</p>
                                <p style='font-size:14px;'>Delivery address: {model.Address}, Rawalpindi</p>
                            </div>
                        </div>
                    </div>";

                    SendSimpleEmail(userEmail, $"Order Confirmed! {createdOrders.Count} order(s) - Artisan Valley", emailBody);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine("Email error: " + emailEx.Message);
                }

                return Json(new { success = true, redirectUrl = "/Cart/OrderConfirmed?id=" + createdOrders[0].OrderId });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                return Json(new { success = false, message = "Error: " + ex.Message + " | Inner: " + inner });
            }
        }
        private static int LevenshteinDistance(string s, string t)
        {
            s = s.ToLowerInvariant();
            t = t.ToLowerInvariant();
            int n = s.Length, m = t.Length;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private List<(string AreaName, decimal DeliveryCharge, string ZoneName, string EstimatedTime)> GetAllAreasWithZones()
        {
            var list = new List<(string, decimal, string, string)>();
            using var con = new Microsoft.Data.SqlClient.SqlConnection(_context.Database.GetConnectionString());
            con.Open();
            var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
        SELECT azm.AreaName, dz.DeliveryCharge, dz.ZoneName, dz.EstimatedTime
        FROM AreaZoneMapping azm
        JOIN DeliveryZones dz ON azm.ZoneId = dz.Id", con);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add((
                    reader["AreaName"].ToString()!,
                    Convert.ToDecimal(reader["DeliveryCharge"]),
                    reader["ZoneName"].ToString()!,
                    reader["EstimatedTime"].ToString()!
                ));
            }
            return list;
        }

        private (string AreaName, decimal DeliveryCharge, string ZoneName, string EstimatedTime)? FindBestAreaMatch(
            string word, List<(string AreaName, decimal DeliveryCharge, string ZoneName, string EstimatedTime)> allAreas)
        {
            word = word.Trim().ToLowerInvariant();
            if (word.Length < 3 || GenericAddressWords.Contains(word)) return null;

            // 1. Exact match
            foreach (var area in allAreas)
                if (area.AreaName.Equals(word, StringComparison.OrdinalIgnoreCase))
                    return area;

            // 2. Substring match (partial typing, jaise "west" -> "Westridge")
            foreach (var area in allAreas)
                if (area.AreaName.ToLowerInvariant().Contains(word) || word.Contains(area.AreaName.ToLowerInvariant()))
                    return area;

            // 3. Fuzzy/typo match (jaise "sadar" -> "Saddar")
            (string AreaName, decimal DeliveryCharge, string ZoneName, string EstimatedTime)? best = null;
            int bestDistance = int.MaxValue;
            int threshold = word.Length <= 4 ? 1 : (word.Length <= 7 ? 2 : 3);

            foreach (var area in allAreas)
            {
                foreach (var areaWord in area.AreaName.Split(' '))
                {
                    int dist = LevenshteinDistance(word, areaWord);
                    if (dist <= threshold && dist < bestDistance)
                    {
                        bestDistance = dist;
                        best = area;
                    }
                }
            }
            return best;
        }
        private decimal CalculateVerifiedDeliveryFee(string address, List<customerseller.Models.CartItem> cartItems, string selectedArea = null)
        {
            if (string.IsNullOrWhiteSpace(address) || cartItems == null || !cartItems.Any())
                return 0;

            var allAreas = GetAllAreasWithZones();
            decimal baseFee = 0;

           
            if (!string.IsNullOrWhiteSpace(selectedArea))
            {
                var chosen = allAreas.FirstOrDefault(a =>
                    a.AreaName.Trim().Equals(selectedArea.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(chosen.AreaName))
                    baseFee = chosen.DeliveryCharge;
            }

           
            if (baseFee <= 0)
            {
                var words = address.Split(new[] { ' ', ',', '-', '.', '/' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(w => w.Length > 2 && !GenericAddressWords.Contains(w))
                                    .ToList();
                foreach (var word in words)
                {
                    var match = FindBestAreaMatch(word, allAreas);
                    if (match != null)
                    {
                        baseFee = match.Value.DeliveryCharge;
                        break;
                    }
                }
            }

            if (baseFee <= 0) return 0;

            var shopGroups = cartItems
                .Select(item =>
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    return product?.SellerEmail ?? "unknown";
                })
                .GroupBy(sellerEmail => sellerEmail)
                .ToList();

            decimal total = 0;
            const decimal SAME_SHOP_EXTRA_CHARGE = 20;

            foreach (var group in shopGroups)
            {
                int extraItems = Math.Max(group.Count() - 1, 0);
                total += baseFee + (extraItems * SAME_SHOP_EXTRA_CHARGE);
            }

            return total;
        }
        [HttpGet]
        public IActionResult TrackOrder(string orderId)
        {
            var currentEmail = HttpContext.Session.GetString("UserEmail");

            
            if (string.IsNullOrEmpty(currentEmail))
            {
                var ret = string.IsNullOrWhiteSpace(orderId)
                    ? "/Cart/TrackOrder"
                    : "/Cart/TrackOrder?orderId=" + Uri.EscapeDataString(orderId.Trim());
                return RedirectToAction("Index", "Home", new { returnUrl = ret });
            }

            
            if (HttpContext.Session.GetString("UserRole") == "Seller")
            {
                TempData["SellerBlockedMsg"] = "Sellers cannot place orders, so there is nothing to track here. Please use a customer account.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(orderId))
                return View("/Views/Cart/TrackOrder.cshtml", null);

            string searchId = orderId.Trim().ToUpper();

            var order = _context.Orders
                .Include(o => o.Items)
                .Include(o => o.TrackingTimeline)
                .FirstOrDefault(o =>
                    o.TrackingId != null && o.TrackingId.Trim().ToUpper() == searchId);

            
            if (order == null ||
                !string.Equals(order.Email?.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["TrackError"] = "No order found with this tracking ID on your account.";
                return View("/Views/Cart/TrackOrder.cshtml", null);
            }

            if (!string.IsNullOrEmpty(order.ParentOrderId))
            {
                ViewBag.RelatedOrders = _context.Orders
                    .Include(o => o.Items)
                    .AsNoTracking()
                    .Where(o => o.ParentOrderId == order.ParentOrderId && o.OrderId != order.OrderId)
                    .ToList();
            }

            return View("/Views/Cart/TrackOrder.cshtml", order);
        }
        [HttpPost]
        public IActionResult CancelOrder(string orderId, string cancelScope = "this")
        {
            if (string.IsNullOrWhiteSpace(orderId)) return BadRequest("Order ID is required");

            var order = _context.Orders
                .Include(o => o.TrackingTimeline)
                .FirstOrDefault(o => o.OrderId.ToUpper() == orderId.ToUpper().Trim());

            if (order == null)
            {
                TempData["TrackError"] = "Order not found.";
                return RedirectToAction("TrackOrder");
            }

            var currentEmail = HttpContext.Session.GetString("UserEmail");

            
            if (string.IsNullOrEmpty(currentEmail))
            {
                return RedirectToAction("Index", "Home",
                    new { returnUrl = "/Cart/TrackOrder?orderId=" + order.TrackingId });
            }

            
            if (!string.Equals(order.Email?.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["TrackError"] = "This order belongs to a different account. Please login with the email used to place it.";
                return RedirectToAction("TrackOrder", new { orderId = order.TrackingId });
            }

            
            var toCancel = new List<Order> { order };
            if (cancelScope == "all" && !string.IsNullOrEmpty(order.ParentOrderId))
            {
                toCancel = _context.Orders
                    .Include(o => o.TrackingTimeline)
                    .Where(o => o.ParentOrderId == order.ParentOrderId)
                    .ToList()
                    .Where(o => string.Equals(o.Email?.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int cancelled = 0;
            foreach (var o in toCancel)
            {
                if (o.Status != "Pending" && o.Status != "Processing") continue;

                o.Status = "Cancelled";
                o.CancelledDate = DateTime.Now;
                o.RiderCompanyId = null;
                o.RiderStatus = null;
                o.TrackingTimeline ??= new List<TrackingStep>();
                o.TrackingTimeline.Add(new TrackingStep
                {
                    OrderId = o.OrderId,
                    Status = "Cancelled",
                    Timestamp = DateTime.Now,
                    Description = "Cancelled by user.",
                    Completed = true,
                    Active = false
                });

                
                var items = _context.Set<CartItem>().Where(i => i.OrderId == o.OrderId).ToList();
                foreach (var item in items)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null && int.TryParse(product.Stock, out var st))
                        product.Stock = (st + item.Quantity).ToString();
                }
                cancelled++;
            }

            if (cancelled == 0)
            {
                TempData["TrackError"] = "This order cannot be cancelled at its current stage.";
                return RedirectToAction("TrackOrder", new { orderId = order.TrackingId });
            }

            _context.SaveChanges();
            return RedirectToAction("TrackOrder", new { orderId = order.TrackingId });
        }
        [HttpPost]
        public IActionResult ReturnOrder(string orderId)
        {
            var order = _context.Orders
    .Include(o => o.TrackingTimeline)
    .Include(o => o.Items)
    .FirstOrDefault(o => o.OrderId == orderId);

            var currentEmail = HttpContext.Session.GetString("UserEmail");
            if (order != null && (string.IsNullOrEmpty(currentEmail) || !string.Equals(order.Email, currentEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return Unauthorized("You are not authorized to return this order.");
            }

            if (order != null && order.Status == "Delivered")
            {
                order.Status = "Return Requested";
                order.TrackingTimeline ??= new List<TrackingStep>();
                order.TrackingTimeline.Add(new TrackingStep
                {
                    OrderId = orderId,
                    Status = "Return Requested",
                    Timestamp = DateTime.Now,
                    Description = "Return requested by customer.",
                    Completed = true,
                    Active = false
                });
                _context.SaveChanges();

                try
                {
                    // Customer ko confirmation
                    if (!string.IsNullOrEmpty(order.Email))
                    {
                        SendSimpleEmail(
                            order.Email,
                            $"Return Request Received — #{order.OrderId}",
                            $@"<div style='font-family:Arial, sans-serif; max-width:500px; margin:auto; padding:20px;'>
                                <h2 style='color:#a64d79;'>Return Request Received</h2>
                                <p>Dear {order.FirstName},</p>
                                <p>We have received your return request for order <strong>#{order.OrderId}</strong>.</p>
                                <p>Our team will review it and contact you soon regarding the next steps.</p>
                                <p>Artisan Valley Team</p>
                            </div>"
                        );
                    }

                    // Admin/store ko alert
                    SendSimpleEmail(
                        _configuration["EmailSettings:FromEmail"],
                        $"Return Requested — #{order.OrderId}",
                        $@"<div style='font-family:Arial, sans-serif; max-width:500px; margin:auto; padding:20px;'>
                            <h2 style='color:#a64d79;'>New Return Request</h2>
                            <p><strong>Order ID:</strong> {order.OrderId}</p>
                            <p><strong>Customer:</strong> {order.CustomerName} ({order.CustomerPhone})</p>
                            <p><strong>Email:</strong> {order.Email}</p>
                            <p><strong>Total:</strong> Rs. {order.Total:N0}</p>
                            <p>Please review this return request in the seller/admin dashboard.</p>
                        </div>"
                    );

                    // Concerned seller(s) ko alert
                    var sellerEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (order.Items != null)
                    {
                        foreach (var item in order.Items)
                        {
                            var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                            if (product != null && !string.IsNullOrEmpty(product.SellerEmail))
                            {
                                sellerEmails.Add(product.SellerEmail);
                            }
                        }
                    }

                    foreach (var sellerEmail in sellerEmails)
                    {
                        SendSimpleEmail(
                            sellerEmail,
                            $"Return Requested for Order #{order.OrderId}",
                            $@"<div style='font-family:Arial, sans-serif; max-width:500px; margin:auto; padding:20px;'>
                                <h2 style='color:#a64d79;'>Return Request on Your Order</h2>
                                <p>A customer has requested a return for order <strong>#{order.OrderId}</strong>, which includes item(s) from your shop.</p>
                                <p><strong>Customer:</strong> {order.CustomerName} ({order.CustomerPhone})</p>
                                <p><strong>Total:</strong> Rs. {order.Total:N0}</p>
                                <p>Please login to your seller dashboard to review and take action.</p>
                            </div>"
                        );
                    }
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine("Return email error: " + emailEx.Message);
                }
            }
            return RedirectToAction("TrackOrder", new { orderId });
        }
        private void SendSimpleEmail(string to, string subject, string body)
        {
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress("Artisan Valley", _configuration["EmailSettings:FromEmail"]));
            message.To.Add(new MimeKit.MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new MimeKit.TextPart("html") { Text = body };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            int smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
            client.Connect(_configuration["EmailSettings:SmtpHost"], smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            client.Authenticate(_configuration["EmailSettings:FromEmail"], _configuration["EmailSettings:AppPassword"]);
            client.Send(message);
            client.Disconnect(true);
        }
        public IActionResult ShoppingCart() => View();

        [HttpGet]
        public IActionResult OrderConfirmed(string id)
        {
            var currentEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(currentEmail))
            {
                var ret = string.IsNullOrWhiteSpace(id)
                    ? "/Cart/OrderConfirmed"
                    : "/Cart/OrderConfirmed?id=" + Uri.EscapeDataString(id.Trim());
                return RedirectToAction("Index", "Home", new { returnUrl = ret });
            }

            if (HttpContext.Session.GetString("UserRole") == "Seller")
            {
                TempData["SellerBlockedMsg"] = "Sellers cannot place orders, so there is no order history here. Please use a customer account.";

                var back = Request.Headers["Referer"].ToString();
                if (Uri.TryCreate(back, UriKind.Absolute, out var uri) &&
                    uri.Host == Request.Host.Host &&
                    !uri.AbsolutePath.StartsWith("/Cart/OrderConfirmed", StringComparison.OrdinalIgnoreCase))
                    return Redirect(uri.PathAndQuery);

                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrEmpty(id))
            {
                var order = _context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.TrackingTimeline)
                    .AsNoTracking()
                    .FirstOrDefault(o => o.OrderId == id);

                if (order != null &&
     string.Equals(order.Email?.Trim(), currentEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    order.Items ??= new List<CartItem>();
                    order.TrackingTimeline ??= new List<TrackingStep>();

                    if (!string.IsNullOrEmpty(order.ParentOrderId))
                    {
                        ViewBag.RelatedOrders = _context.Orders
                            .Include(o => o.Items)
                            .AsNoTracking()
                            .Where(o => o.ParentOrderId == order.ParentOrderId && o.OrderId != order.OrderId)
                            .ToList();
                    }
                    return View(order);
                }
            }

            return View();
        }
        public IActionResult GetAreaList()
        {
            var areas = GetAllAreasWithZones()
                .Select(a => new { name = a.AreaName, charge = a.DeliveryCharge, zone = a.ZoneName })
                .OrderBy(a => a.name)
                .ToList();
            return Json(areas);
        }
        public IActionResult GetDeliveryCharge(string area)
        {
            if (string.IsNullOrEmpty(area))
                return Json(new { charge = 0, zone = "", time = "" });

            var allAreas = GetAllAreasWithZones();
            var match = FindBestAreaMatch(area, allAreas);

            if (match != null)
            {
                return Json(new
                {
                    charge = match.Value.DeliveryCharge,
                    zone = match.Value.ZoneName,
                    time = match.Value.EstimatedTime,
                    found = true
                });
            }

            return Json(new { charge = 0, zone = "", time = "", found = false });
        }
        private static readonly HashSet<string> GenericAddressWords = new(StringComparer.OrdinalIgnoreCase)
{
    "rawalpindi", "pindi", "pakistan", "road", "street", "house", "block",
    "sector", "phase", "near", "gali", "mohalla", "city", "town", "colony",
    "chowk", "bazar", "bazaar", "market"
};
       
        private static string GenericWordsSqlList()
        {
            return string.Join(",", GenericAddressWords.Select(w => "'" + w.Replace("'", "''") + "'"));
        }
        public IActionResult GetAreas(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 3)
                return Json(new List<string>());

            if (GenericAddressWords.Contains(term.Trim()))
                return Json(new List<string>());

            var allAreas = GetAllAreasWithZones();
            var match = FindBestAreaMatch(term, allAreas);

            return Json(match != null ? new List<string> { match.Value.AreaName } : new List<string>());
        }
        [HttpGet]
        public IActionResult GetOrderForComplaint(string orderId)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { found = false, message = "Please login first." });

            if (HttpContext.Session.GetString("UserRole") == "Seller")
                return Json(new { found = false, message = "Sellers cannot file complaints." });

            if (string.IsNullOrWhiteSpace(orderId))
                return Json(new { found = false, message = "Please enter an Order ID." });

            var id = orderId.Trim().ToUpper();

            var order = _context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefault(o => o.OrderId.ToUpper() == id);

            if (order == null || !string.Equals(order.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
                return Json(new { found = false, message = "No order found with this ID on your account." });
if (order.Status != "Delivered")
                return Json(new { found = false, message = "You can only file a complaint after your order is delivered. Current status: " + order.Status + "." });
            var shopName = order.Items?.FirstOrDefault()?.SellerShopName ?? "";

            return Json(new
            {
                found = true,
                orderId = order.OrderId,
                shopName,
                amount = order.Total.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                date = order.OrderDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        [HttpGet]
        public IActionResult Complaint(string? orderId, string? amount)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Index", "Home", new { returnUrl = "/Cart/Complaint" });

            if (HttpContext.Session.GetString("UserRole") == "Seller" ||
      _context.SellerShops.Any(s => s.SellerEmail.ToLower() == userEmail.ToLower()))
            {
                TempData["SellerBlockedMsg"] = "Sellers cannot file complaints. Please use a customer account.";


                var back = Request.Headers["Referer"].ToString();
                if (Uri.TryCreate(back, UriKind.Absolute, out var uri) &&
                    uri.Host == Request.Host.Host &&
                    !uri.AbsolutePath.StartsWith("/Cart/Complaint", StringComparison.OrdinalIgnoreCase))
                    return Redirect(uri.PathAndQuery);

                return RedirectToAction("Index", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            ViewBag.OrderId = orderId ?? "";
            ViewBag.Amount = amount ?? "";
            ViewBag.CustomerName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "";
            ViewBag.CustomerEmail = userEmail;

            return View();
        }
    }
}