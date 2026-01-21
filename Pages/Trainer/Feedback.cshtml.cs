using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class FeedbackModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public FeedbackModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerFeedback> Feedbacks { get; set; } = new();

        public async Task OnGetAsync()
        {
            int trainerUserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null)
            {
                Feedbacks = new();
                return;
            }

            Feedbacks = await _db.TrainerFeedbacks
                .Include(f => f.User)
                .Where(f => f.TrainerProfileId == trainerProfile.Id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }
}
