using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

namespace FitQuest.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required, MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        public int Duration { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow; 

        [Required]
        public ActivityStatus Status { get; set; } = ActivityStatus.Pending; 

        public ICollection<Evidence>? Evidences { get; set; }

        public int FullXp { get; set; }    
        public bool XpAwarded { get; set; } = false;

        public bool IsTrainerAssigned { get; set; } = false;
    }

    public enum ActivityStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Expired = 3
    }
}
