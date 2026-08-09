using Microsoft.AspNetCore.Mvc;
using customerseller.Models;
using Microsoft.EntityFrameworkCore;

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

            // Orders ke saath Items bhi load karo
            var orders = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Items != null && o.Items.Any(i => sellerProductIds.Contains(i.ProductId)))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Har order mein seller ke products ki info set karo
            foreach (var order in orders)
            {
                var sellerItem = order.Items?.FirstOrDefault(i => sellerProductIds.Contains(i.ProductId));
                if (sellerItem != null)
                {
                    order.ProductName = sellerItem.Title;
                    order.Price = sellerItem.Price * sellerItem.Quantity;
                }
                // CustomerName set karo
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

            // WhatsApp message generate karo
            var subtotal = order.Total - (order.DeliveryFee ?? 0);
            var whatsappMsg = $@"Assalam o Alaikum! 🎉
Your order is on its way!

📦 Order: {order.ProductName ?? "Your items"}
💰 Amount: Rs.{order.Total:N0}

🛵 Our rider will deliver your order soon.
📞 Rider Contact: {courier.Phone}

Thank you for shopping with Artisan Valley! 🌸";

            return Json(new
            {
                success = true,
                whatsappMessage = whatsappMsg,
                customerPhone = customerPhone
            });
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