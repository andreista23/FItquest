using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class AdminAccessCode
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string CodeHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; } // opțional
    }
}