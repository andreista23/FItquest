using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class FeedbackFromTrainerModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public FeedbackFromTrainerModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerFeedback> Feedbacks { get; set; } = new();

        public async Task OnGetAsync()
        {
            int userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            Feedbacks = await _db.TrainerFeedbacks
                .Include(f => f.TrainerProfile)
                .ThenInclude(tp => tp.User)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }
}
