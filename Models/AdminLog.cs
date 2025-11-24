using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class AdminLog
    {
        public int Id { get; set; } // log_id

        [Required]
        public int AdminId { get; set; }
        public User Admin { get; set; }

        [Required, MaxLength(200)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Target { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
