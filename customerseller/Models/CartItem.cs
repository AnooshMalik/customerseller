using System.ComponentModel.DataAnnotations.Schema;

namespace customerseller.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string ProductId { get; set; } 
        public int DisplayId { get; set; }   
        public string Title { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public string? OrderId { get; set; }
        public string? SellerShopName { get; set; }
    }
}