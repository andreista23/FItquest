using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class Badge
    {
        public int Id { get; set; } // badge_id

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Criteria { get; set; }

        public ICollection<UserBadge>? UserBadges { get; set; }
    }
}
