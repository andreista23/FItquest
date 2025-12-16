using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class XPEvent
    {
        public int Id { get; set; } 

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        public int? ActivityId { get; set; }
        public Activity? Activity { get; set; }

        [Required]
        public int XPValue { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
