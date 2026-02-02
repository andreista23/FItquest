using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class Quest
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int RewardXP { get; set; }

        // "daily", "weekly", "lifetime" etc.
        [MaxLength(50)]
        public string Period { get; set; } = "lifetime";

        // ✅ nou
        public QuestType Type { get; set; }

        //X zile / X minute / X activități
        public int Target { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
