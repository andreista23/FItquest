using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class Evidence
    {
        public int Id { get; set; } // evidence_id

        [Required]
        public int ActivityId { get; set; }
        public Activity Activity { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

        public bool Validated { get; set; } = false;
    }
}
