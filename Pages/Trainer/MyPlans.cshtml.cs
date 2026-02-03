using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [BindProperty]
        public int SelectedUserId { get; set; }

        [BindProperty]
        public int SelectedTrainerActivityId { get; set; }

        [BindProperty]
        public int Times { get; set; } = 1;

        public List<FitnessPlan> FitnessPlans { get; set; } = new();
        public List<TrainerSubscriptionPlan> SubscriptionPlans { get; set; } = new();

        // ✅ pentru dropdown-uri (asp-items)
        public List<SelectListItem> Subscribers { get; set; } = new();
        public List<SelectListItem> MyActivities { get; set; } = new();

        public async Task OnGetAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null)
            {
                FitnessPlans = new();
                SubscriptionPlans = new();
                Subscribers = new();
                MyActivities = new();
                return;
            }

            // 🗂 Fitness plans (cu items + activități)
            FitnessPlans = await _db.FitnessPlans
                .Where(p => p.TrainerProfileId == trainerProfile.Id)
                .Include(p => p.Items)
                    .ThenInclude(i => i.TrainerActivity)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // 💳 Subscription plans
            SubscriptionPlans = await _db.TrainerSubscriptionPlans
                .Where(p => p.TrainerProfileId == trainerProfile.Id)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // ✅ ACTIVIȚĂȚILE TRAINER-ULUI pentru dropdown
            MyActivities = await _db.TrainerActivities
                .Where(a => a.TrainerProfileId == trainerProfile.Id)
                .OrderBy(a => a.Title)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Title
                })
                .ToListAsync();

            // ✅ ABONAȚII ACTIVI pentru dropdown
            // NOTĂ: aici trebuie să adaptăm la modelul tău real de "abonament activ"
            // Dacă ai tabel "TrainerSubscriptions" sau "Subscriptions", schimbă query-ul.
            Subscribers = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerUserId && s.Status == "active")
                .Select(s => new { s.UserId, Name = s.User.Email })
                .Distinct()
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.UserId.ToString(), Text = x.Name })
                .ToListAsync();
        }
    }
}