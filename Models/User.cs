using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public UserRole Role { get; set; } = UserRole.Standard;

        public string? GoogleId { get; set; }

        public string? PasswordHash { get; set; }

        public int Xp { get; set; } = 0;

        public int LastNotifiedLevel { get; set; } = 0;

        public bool IsBanned { get; set; } = false;

        public TrainerProfile? TrainerProfile { get; set; }

    }
}