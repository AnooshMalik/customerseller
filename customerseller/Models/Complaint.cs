namespace customerseller.Models
{
    public class Complaint
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? OrderId { get; set; }
        public string? AmountPaid { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string ComplaintType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidenceImageUrl { get; set; }
        public string Status { get; set; } = "Open";
        public string? AdminNotes { get; set; }
        public string? SellerResponse { get; set; }
        public string? SellerProofImageUrl { get; set; }
        public bool? IsResolvedByCustomer { get; set; }
        public int CustomerStrike { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}