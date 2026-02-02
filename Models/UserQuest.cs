using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class UserQuest
    {
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int QuestId { get; set; }
        public Quest Quest { get; set; }

        [MaxLength(20)]
        public string State { get; set; } = "active"; // active / completed

        public int Progress { get; set; } = 0;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
