using System;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class Friendship
    {
        public int Id { get; set; }

        // cine a trimis cererea
        [Required]
        public int RequesterId { get; set; }
        public User Requester { get; set; } = null!;

        // cine primește cererea
        [Required]
        public int AddresseeId { get; set; }
        public User Addressee { get; set; } = null!;

        // "Pending", "Accepted", "Declined", "Blocked"
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}