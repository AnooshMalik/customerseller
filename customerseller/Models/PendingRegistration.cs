namespace customerseller.Models
{
    public class PendingRegistration
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string Token { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}