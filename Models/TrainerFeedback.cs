using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class TrainerFeedback
    {
        public int Id { get; set; }

        [Required]
        public int TrainerProfileId { get; set; }
        public TrainerProfile TrainerProfile { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
