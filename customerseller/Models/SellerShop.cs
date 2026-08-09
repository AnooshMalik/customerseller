namespace customerseller.Models
{
    public class SellerShop
    {
        public int Id { get; set; }
        public string SellerEmail { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string CNIC { get; set; } = string.Empty;
        public string SubCategories { get; set; } = string.Empty;
        public bool TermsAccepted { get; set; }
        public DateTime? SubscriptionStart { get; set; }
        public DateTime? SubscriptionEnd { get; set; }
        public bool IsPaid { get; set; }
        public bool IsApproved { get; set; } = false;
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Video
        public string? VideoPath { get; set; }

        // Moderator fields
        public string? ModeratorStatus { get; set; } = "Pending";
        public string? ModeratorComments { get; set; }
        public string? ModeratorEmail { get; set; }
        public DateTime? ModeratorReviewedAt { get; set; }

        // Admin fields
        public string? AdminStatus { get; set; } = "Pending";
        public string? RejectionReason { get; set; }
        public string? SellerAddress { get; set; }
        public string? SellerArea { get; set; }
        public string? SellerPhone { get; set; }

        public string? RejectedSubCategories { get; set; }
        public string? SubCategoryRejectionReasons { get; set; }
    }
}