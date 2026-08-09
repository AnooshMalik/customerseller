using Microsoft.AspNetCore.Mvc;
using customerseller.Models;
using System.Collections.Generic;
using System.Transactions;

namespace customerseller.Controllers.seller
{
    public class PaymentsController : Controller
    {
        public IActionResult Payments()
        {
            // Bilkul clean model initialize karein
            var viewModel = new PaymentViewModel
            {
                CashInHand = 0,
                InTransitCash = 0,
                TotalCODSales = 0,
                Transactions = new List<Payments>() // Khali list
            };

            return View("~/Views/Seller/Payments/payments.cshtml", viewModel);
        }
    }
}