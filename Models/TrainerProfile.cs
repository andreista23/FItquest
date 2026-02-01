using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class TrainerProfile
    {
        public int Id { get; set; }

        // 🔗 legătură cu User
        public int UserId { get; set; }
        public User User { get; set; }

        // 📄 documente pentru register trainer
        public string CvPath { get; set; } = string.Empty;
        public string RecommendationPath { get; set; } = string.Empty;

        // aprobare trainer (Sprint 5 – Admin)
        public bool IsApproved { get; set; } = false;

        // ✅ CE AVEAI TU – RĂMÂNE
        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; }

        public double RatingAvg { get; set; } = 0;

        public ICollection<Subscription>? Subscriptions { get; set; }
    }
}
