using System;

namespace customerseller.Models
{
    public class Review
    {
        
        public int Id { get; set; }

        
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }

        
        public string? UserName { get; set; }
        public string? CustomerName { get; set; }

       
        public string? CustomerInitials { get; set; }

       
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? SellerResponse { get; set; }

     
        public DateTime CreatedAt { get; set; }
        public string? DateLabel { get; set; }
    }
}