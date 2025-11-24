using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class UserBadge
    {
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int BadgeId { get; set; }
        public Badge Badge { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
