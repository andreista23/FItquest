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
    public class CreatePlanModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CreatePlanModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public TrainerSubscriptionPlan Plan { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstAsync(t => t.UserId == trainerUserId);

            Plan.TrainerProfileId = trainerProfile.Id;

            _db.TrainerSubscriptionPlans.Add(Plan);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Trainer/Dashboard");
        }
    }
}
