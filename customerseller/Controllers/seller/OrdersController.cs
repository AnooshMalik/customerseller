using Microsoft.AspNetCore.Mvc;
using customerseller.Models;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;

namespace customerseller.Controllers.seller
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Order()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);

            var sellerProductIds = _context.Products
                .Where(p => p.SellerEmail == email)
                .Select(p => p.Id)
                .ToList();

            var orders = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Items != null && o.Items.Any(i => sellerProductIds.Contains(i.ProductId)))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

           
            foreach (var order in orders)
            {
                var sellerItem = order.Items?.FirstOrDefault(i => sellerProductIds.Contains(i.ProductId));
                if (sellerItem != null)
                {
                    order.ProductName = sellerItem.Title;
                    order.Price = sellerItem.Price * sellerItem.Quantity;
                }
               
                if (string.IsNullOrEmpty(order.CustomerName))
                    order.CustomerName = (order.FirstName + " " + order.LastName).Trim();

                var customerUser = _context.Users.FirstOrDefault(u => u.Email == order.Email);
                order.CustomerPhone = order.CustomerPhone ?? customerUser?.Phone ?? order.Email ?? "";
                order.CustomerAddress = (order.Address ?? "").Replace("'", " ");
            }

            ViewBag.Couriers = _context.CourierCompanies
                .Where(c => c.IsActive)
                .ToList();

            ViewBag.SellerShop = shop;

            return View("~/Views/Seller/Orders/Order.cshtml", orders);
        }

        [HttpPost]
        public IActionResult AssignToDelivery(string orderId, int courierId, string customerPhone, string customerAddress)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { error = "Unauthorized" });

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return Json(new { error = "Order not found" });

            var shop = _context.SellerShops.FirstOrDefault(s => s.SellerEmail == email);
            var courier = _context.CourierCompanies.FirstOrDefault(c => c.Id == courierId);

            if (shop == null || courier == null)
                return Json(new { error = "Shop or Courier not found" });

            order.CourierCompanyId = courierId;
            order.CourierStatus = "Assigned";
            order.CourierAssignedAt = DateTime.Now;
            order.SellerPickupAddress = $"{shop.SellerAddress}, {shop.SellerArea}";
            order.SellerPhone = shop.SellerPhone;
            order.CustomerPhone = customerPhone;
            order.CustomerAddress = customerAddress;



            _context.SaveChanges();

           
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Artisan Valley", "artisanvalley.store@gmail.com"));
                message.To.Add(new MailboxAddress("", order.Email));
                message.Subject = $"Your Order #{order.OrderId} is on its way! - Artisan Valley";
                message.Body = new TextPart("html")
                {
                    Text = $@"<div style='font-family:sans-serif; max-width:500px; margin:auto;'>
            <h2 style='color:#a64d79;'>Your Order is on its way! 🎉</h2>
            <p>Assalam o Alaikum {order.FirstName},</p>
            <p><strong>Order ID:</strong> {order.OrderId}</p>
            <p><strong>Item:</strong> {order.ProductName ?? "Your items"}</p>
            <p><strong>Amount:</strong> Rs. {order.Total:N0}</p>
            <p><strong>Delivery Address:</strong> {order.CustomerAddress}</p>
            <hr style='margin:15px 0; border:none; border-top:1px solid #eee;'>
            <p>🛵 Our rider will deliver your order soon.</p>
            <p><strong>Rider Contact:</strong> {courier.Phone}</p>
            <p><strong>Courier Company:</strong> {courier.CompanyName}</p>
            <hr style='margin:15px 0; border:none; border-top:1px solid #eee;'>
            <p>Thank you for shopping with Artisan Valley! 🌸</p>
        </div>"
                };

                using var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate("artisanvalley.store@gmail.com", "esphtmdtnnptlhuo");
                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception emailEx)
            {
                Console.WriteLine("Email error: " + emailEx.Message);
            }

            var subtotal = order.Total - (order.DeliveryFee ?? 0);
            var whatsappMsg = $@"Assalam o Alaikum! 🎉
Your order is on its way!

📦 Order: {order.ProductName ?? "Your items"}
💰 Amount: Rs.{order.Total:N0}

🛵 Our rider will deliver your order soon.
📞 Rider Contact: {courier.Phone}

Thank you for shopping with Artisan Valley! 🌸";

            return Json(new { success = true });
        }

        public IActionResult GetOrderCount()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { count = 0 });

            var sellerProductIds = _context.Products
                .Where(p => p.SellerEmail == email)
                .Select(p => p.Id)
                .ToList();

            var count = _context.Orders
                .Where(o => o.Items != null && o.Items.Any(i => sellerProductIds.Contains(i.ProductId)))
                .Count();

            return Json(new { count = count });
        }
    }
}