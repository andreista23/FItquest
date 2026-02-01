using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class PremiumRequest
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string PaymentProofPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Pending / Approved / Rejected
    }
}
