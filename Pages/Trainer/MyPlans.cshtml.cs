using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class MyPlansModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public MyPlansModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<FitnessPlan> FitnessPlans { get; set; } = new();
        public List<TrainerSubscriptionPlan> SubscriptionPlans { get; set; } = new();

        public async Task OnGetAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null)
            {
                FitnessPlans = new();
                SubscriptionPlans = new();
                return;
            }

            // 🗂 Fitness plans (cu items + activități)
            FitnessPlans = await _db.FitnessPlans
                .Where(p => p.TrainerProfileId == trainerProfile.Id)
                .Include(p => p.Items)
                    .ThenInclude(i => i.TrainerActivity)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // 💳 Subscription plans (ce vede userul la Premium/ChooseTrainer)
            SubscriptionPlans = await _db.TrainerSubscriptionPlans
                .Where(p => p.TrainerProfileId == trainerProfile.Id)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }
    }
}