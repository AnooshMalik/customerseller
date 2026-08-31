using System.ComponentModel.DataAnnotations;

namespace customerseller.Models
{
    public class CheckoutViewModel
    {
       
        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^03\d{9}$", ErrorMessage = "Please enter a valid mobile number")]
        public string Email { get; set; }

        
        public string Country { get; set; } = "Pakistan";
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string? Apartment { get; set; }
        public string City { get; set; }
        public string? PostalCode { get; set; }
        public bool SaveInformation { get; set; }

        
        public string PaymentMethod { get; set; } = "COD";

        
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    

    public class OrderViewModel
    {
        public string OrderId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string ShippingAddress { get; set; }
        public string City { get; set; }
        public string PaymentMethod { get; set; }
        public List<CartItem> Items { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending";
    }
}