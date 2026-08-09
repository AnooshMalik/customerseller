using customerseller.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace customerseller.Models
{
    public class Order
    {
        [Key]
        public string OrderId { get; set; } = "";
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PaymentMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string? Status { get; set; }
        public string? TrackingId { get; set; }
        public string? CustomerName { get; set; }
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }

        public List<CartItem> Items { get; set; } = new();
        public List<TrackingStep> TrackingTimeline { get; set; } = new();

        public int? CourierCompanyId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierStatus { get; set; }
        public DateTime? CourierAssignedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public bool? PaymentReceived { get; set; }
        public string? PaymentNotReceivedReason { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? SellerPickupAddress { get; set; }
        public string? SellerPhone { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal? DeliveryFee { get; set; }
    }
}