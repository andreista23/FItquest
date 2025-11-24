using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

namespace FitQuest.Models
{
    public class Activity
    {
        public int Id { get; set; } // activity_id

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required, MaxLength(100)]
        public string Type { get; set; } = string.Empty; // type

        [Required]
        public int Duration { get; set; } // duration (în minute)

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow; // date

        [Required]
        public ActivityStatus Status { get; set; } = ActivityStatus.Pending; // status

        public ICollection<Evidence>? Evidences { get; set; }
    }

    public enum ActivityStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
