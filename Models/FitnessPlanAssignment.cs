using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class FitnessPlanAssignment
    {
        public int Id { get; set; }

        [Required]
        public int FitnessPlanId { get; set; }
        public FitnessPlan FitnessPlan { get; set; } = null!;

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}