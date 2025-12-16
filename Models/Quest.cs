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

        [MaxLength(100)]
        public string? Period { get; set; }

        public ICollection<UserQuest>? UserQuests { get; set; }
    }
}
