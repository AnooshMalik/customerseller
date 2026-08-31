using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace customerseller.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string? AdditionalImages { get; set; }
        public string? Description { get; set; }
        public string? Rating { get; set; }
        public string? Reviews { get; set; }
        public string? Sizes { get; set; }
        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public string? DisplayId { get; set; }
        public string? DisplayName { get; set; }
        public string? Stock { get; set; }
        public string? VideoUrl { get; set; }
        public string? VideoStatus { get; set; } = "Pending";
        public string? SellerEmail { get; set; }
        public string? SellerName { get; set; }
        public string? RejectionReason { get; set; }
        public string? InternalNotes { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public string? Brand { get; set; }
        public bool IsAdminApproved { get; set; } = false;
        public DateTime? AdminApprovedAt { get; set; }
        public string? Color { get; set; }
        public string? AdminRejectionReason { get; set; }

       
        public string? Status { get; set; } = "Pending";
    }
}
