using System.ComponentModel.DataAnnotations;

namespace FitQuest.Models
{
    public class UserLoginDay
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        // doar data (UTC)
        public DateTime DayUtc { get; set; }
    }
}
