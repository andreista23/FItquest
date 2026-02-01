using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class CreateFitnessPlanModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CreateFitnessPlanModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public FitnessPlan Plan { get; set; } = new();

        [BindProperty]
        public List<int> SelectedActivityIds { get; set; } = new();

        public List<TrainerActivity> MyActivities { get; set; } = new();

        public async Task OnGetAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var trainerProfile = await _db.TrainerProfiles.FirstAsync(t => t.UserId == trainerUserId);

            MyActivities = await _db.TrainerActivities
                .Where(a => a.TrainerProfileId == trainerProfile.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var trainerProfile = await _db.TrainerProfiles.FirstAsync(t => t.UserId == trainerUserId);

            Plan.TrainerProfileId = trainerProfile.Id;
            _db.FitnessPlans.Add(Plan);
            await _db.SaveChangesAsync();

            int order = 1;
            foreach (var actId in SelectedActivityIds)
            {
                _db.FitnessPlanItems.Add(new FitnessPlanItem
                {
                    FitnessPlanId = Plan.Id,
                    TrainerActivityId = actId,
                    Order = order++
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToPage("/Trainer/Dashboard");
        }
    }
}
