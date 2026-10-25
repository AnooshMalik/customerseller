namespace customerseller.Models
{
    public class CustomerOrderRow
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string ProductTitle { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = "";
        public DateTime OrderDate { get; set; }
    }
}