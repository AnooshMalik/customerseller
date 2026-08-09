using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace customerseller.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public string SenderEmail { get; set; } = string.Empty;
        [Required]
        public string SenderRole { get; set; } = string.Empty;

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public bool IsEdited { get; set; }
    }
}