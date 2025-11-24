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

        [MaxLength(50)]
        public string State { get; set; } = "pending";

        public int Progress { get; set; } = 0;
    }
}
