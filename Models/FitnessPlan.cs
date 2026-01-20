using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class FitnessPlan
    {
        public int Id { get; set; }

        [Required]
        public int TrainerProfileId { get; set; }
        public TrainerProfile TrainerProfile { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(600)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FitnessPlanItem> Items { get; set; } = new List<FitnessPlanItem>();
    }
}
