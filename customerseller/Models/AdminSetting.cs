namespace customerseller.Models
{
    public class AdminSetting
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = ""; // BCrypt hashed
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}