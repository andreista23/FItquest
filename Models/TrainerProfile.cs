using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class TrainerProfile
    {
        public int Id { get; set; } 

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; }

        public double RatingAvg { get; set; } = 0;

        public ICollection<Subscription>? Subscriptions { get; set; }
    }
}
