using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class Subscription
    {
        public int Id { get; set; } // subscription_id

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int TrainerId { get; set; }
        public TrainerProfile Trainer { get; set; }

        [Required, MaxLength(100)]
        public string PlanType { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "active";
    }
}
