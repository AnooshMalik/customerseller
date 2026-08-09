namespace customerseller.Models
{
    public class TrackingStep
    {
        public int Id { get; set; }
        public string OrderId { get; set; }
        public string Status { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Description { get; set; }
        public bool Completed { get; set; }
        public bool Active { get; set; }
    }
}