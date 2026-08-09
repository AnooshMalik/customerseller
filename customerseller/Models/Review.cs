using System;

namespace customerseller.Models
{
    public class Review
    {
        // Primary Key (Dono projects ki IDs ko handle karne ke liye)
        public int Id { get; set; }

        // Product aur Customer ki details
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }

        // Customer ka naam ( mein UserName hai, SELLERMVC mein CustomerName)
        public string? UserName { get; set; }
        public string? CustomerName { get; set; }

        // Seller Dashboard ke UI ke liye (Initials jaise "Ali Khan" ka "AK")
        public string? CustomerInitials { get; set; }

        // Rating aur Comment
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? SellerResponse { get; set; }

        // Dates (Database ke liye DateTime aur UI par dikhane ke liye Label)
        public DateTime CreatedAt { get; set; }
        public string? DateLabel { get; set; }
    }
}