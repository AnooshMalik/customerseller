
using System.ComponentModel.DataAnnotations;

namespace customerseller.Models
{
    public class CourierCompany
    {
        [Key]
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    
    }
}