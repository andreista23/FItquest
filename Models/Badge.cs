using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class Badge
    {
        public int Id { get; set; } 

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Criteria { get; set; }

        [MaxLength(300)]
        public string? ImagePath { get; set; }

        public ICollection<UserBadge>? UserBadges { get; set; }
    }
}
