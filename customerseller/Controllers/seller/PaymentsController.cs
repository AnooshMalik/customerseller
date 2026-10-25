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
            if (HttpContext.Session.GetString("UserRole") != "Seller")
                return RedirectToAction("Index", "Home");
            var sellerProductIds = _context.Products
                .Where(p => p.SellerEmail == email)
                .Select(p => p.Id)
                .ToList();

            var orders = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Items != null && o.Items.Any(i => sellerProductIds.Contains(i.ProductId))
                       && o.Status != "Cancelled")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            decimal cashInHand = 0;
            decimal inTransitCash = 0;
            decimal pendingCash = 0;
            decimal totalCodSales = 0;
            var transactions = new List<Payments>();

            foreach (var order in orders)
            {
                var sellerItems = order.Items.Where(i => sellerProductIds.Contains(i.ProductId)).ToList();
                if (!sellerItems.Any()) continue;

                decimal amount = sellerItems.Sum(i => i.Price * i.Quantity);
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
                else if (order.Status == "Delivered")
                {
                    inTransitCash += amount;
                    totalCodSales += amount;
                    transactions.Add(new Payments
                    {
                        OrderId = order.OrderId,
                        CustomerName = customerName,
                        Amount = amount,
                        Status = "Delivered, awaiting cash"
                    });
                }
                else if (order.RiderStatus == "Assigned" || order.RiderStatus == "PickedUp")
                {
                    inTransitCash += amount;
                    totalCodSales += amount;
                    transactions.Add(new Payments
                    {
                        OrderId = order.OrderId,
                        CustomerName = customerName,
                        Amount = amount,
                        Status = "In-Transit"
                    });
                }
                else
                {
                    pendingCash += amount;
                    totalCodSales += amount;
                    transactions.Add(new Payments
                    {
                        OrderId = order.OrderId,
                        CustomerName = customerName,
                        Amount = amount,
                        Status = "Processing"
                    });
                }
            }

            var viewModel = new PaymentViewModel
            {
                CashInHand = cashInHand,
                InTransitCash = inTransitCash,
                PendingCash = pendingCash,
                TotalCODSales = totalCodSales,
                Transactions = transactions
            };

            return View("~/Views/Seller/Payments/payments.cshtml", viewModel);
        }
    }
}