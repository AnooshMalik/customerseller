using System.ComponentModel.DataAnnotations;

namespace customerseller.Models
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
        [RegularExpression(@"^[A-Za-z\s.'-]+$", ErrorMessage = "Name can only contain letters, no numbers or symbols")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(\+92|0)3\d{9}$", ErrorMessage = "Please enter a valid number (e.g. 03001234567 or +923001234567)")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 100 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 1000 characters")]
        public string Message { get; set; } = string.Empty;
    }
}