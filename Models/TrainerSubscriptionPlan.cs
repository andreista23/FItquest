using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class TrainerSubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        public int TrainerProfileId { get; set; }
        public TrainerProfile TrainerProfile { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public int DurationDays { get; set; } = 30;

        public bool IsActive { get; set; } = true;
    }
}
