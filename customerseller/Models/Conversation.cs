using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace customerseller.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string User1Email { get; set; } = string.Empty;
        [Required]
        public string User1Role { get; set; } = string.Empty;

        [Required]
        public string User2Email { get; set; } = string.Empty;
        [Required]
        public string User2Role { get; set; } = string.Empty;

        public string? RelatedProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastMessageAt { get; set; } = DateTime.Now;

       
        public DateTime? User1ClearedAt { get; set; }

        
        public DateTime? User2ClearedAt { get; set; }
    }
}