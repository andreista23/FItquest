using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class PlansFromTrainerModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public PlansFromTrainerModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<FitnessPlan> Plans { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var sub = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (sub == null) return;

            Plans = await _db.FitnessPlans
                .Include(p => p.Items)
                .ThenInclude(i => i.TrainerActivity)
                .Where(p => p.TrainerProfileId == sub.TrainerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
