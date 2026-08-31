namespace customerseller.Models
{
    public class PaymentSubmission
    {
        public int Id { get; set; }
        public string SellerEmail { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public decimal Amount { get; set; }
        public string SenderNumber { get; set; } = "";
        public string PaymentMethod { get; set; } = "JazzCash";
        public string? ScreenshotPath { get; set; }
        public string Status { get; set; } = "Pending";
        public string? RejectionReason { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
    }
}