namespace customerseller.Models
{
    public class DashboardViewModel
    {
        public string? SellerName { get; set; }
        public string? ShopName { get; set; }
        public string? AccountStatus { get; set; }
        public decimal NetRevenue { get; set; }
        public int RevenueGrowth { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public double Rating { get; set; }
        public int LiveVisitors { get; set; }
        public List<SaleActivity> RecentSales { get; set; } = new();
        public List<SellerOrderItem> SellerOrders { get; set; } = new();
    }

    public class SaleActivity
    {
        public string? Detail { get; set; }
        public string? Amount { get; set; }
    }

    public class SellerOrderItem
    {
        public string? OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string? ProductTitle { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}