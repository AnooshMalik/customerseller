namespace customerseller.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Buyer";
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public string? Phone { get; set; }
        public bool IsSeller { get; set; }

        public bool IsVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime? BlockExpiry { get; set; }
        public string? BlockReason { get; set; }
    }
}