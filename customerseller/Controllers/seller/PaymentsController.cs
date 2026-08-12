using customerseller.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace customerseller.Controllers.seller
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Payments()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var sellerProductIds = _context.Products
                .Where(p => p.SellerEmail == email)
                .Select(p => p.Id)
                .ToList();

            var orders = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Items != null && o.Items.Any(i => sellerProductIds.Contains(i.ProductId)))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            decimal cashInHand = 0;
            decimal inTransitCash = 0;
            decimal totalCodSales = 0;
            var transactions = new List<Payments>();

            foreach (var order in orders)
            {
                var sellerItem = order.Items.FirstOrDefault(i => sellerProductIds.Contains(i.ProductId));
                if (sellerItem == null) continue;

                decimal amount = sellerItem.Price * sellerItem.Quantity;
                string customerName = order.CustomerName ?? $"{order.FirstName} {order.LastName}".Trim();

                if (order.Status == "Delivered" && order.PaymentReceived == true)
                {
                    cashInHand += amount;
                    totalCodSales += amount;
                    transactions.Add(new Payments
                    {
                        OrderId = order.OrderId,
                        CustomerName = customerName,
                        Amount = amount,
                        Status = "Collected"
                    });
                }
                else if (order.CourierStatus == "Assigned" || order.CourierStatus == "PickedUp")
                {
                    inTransitCash += amount;
                    transactions.Add(new Payments
                    {
                        OrderId = order.OrderId,
                        CustomerName = customerName,
                        Amount = amount,
                        Status = "In-Transit"
                    });
                }
            }

            var viewModel = new PaymentViewModel
            {
                CashInHand = cashInHand,
                InTransitCash = inTransitCash,
                TotalCODSales = totalCodSales,
                Transactions = transactions
            };

            return View("~/Views/Seller/Payments/payments.cshtml", viewModel);
        }
    }
}