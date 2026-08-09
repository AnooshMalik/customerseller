namespace customerseller.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CNIC { get; set; }
        public string? Categories { get; set; }
        public string? Area { get; set; }
    }
}
