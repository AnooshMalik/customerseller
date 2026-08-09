using System.Collections.Generic;

namespace customerseller.Models
{
    public class Payments
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } // "Collected" or "In-Transit"
    }

    public class PaymentViewModel
    {
        public decimal CashInHand { get; set; }
        public decimal InTransitCash { get; set; }
        public decimal TotalCODSales { get; set; }
        public List<Payments> Transactions { get; set; }
    }
}