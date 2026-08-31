namespace customerseller.Models
{
    public class ShopActionModel
    {
        public int Id { get; set; }
        public string? StringId { get; set; }
        public string? Remarks { get; set; }
        public string? Reason { get; set; }
    }
    public class ProductActionModel
    {
        public string Id { get; set; }
        public string Reason { get; set; }
    }
    public class BlockUserModel
    {
        public int Id { get; set; }
        public int? Days { get; set; }

        public string Reason { get; set; }
    }
}