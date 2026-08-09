namespace customerseller.Models
{
    public class SubCategoryVideo
    {
        public int Id { get; set; }
        public string SellerEmail { get; set; } = "";
        public string SubCategory { get; set; } = "";
        public string? VideoUrl { get; set; }
        public string? VideoStatus { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? UploadedAt { get; set; }
    }
}