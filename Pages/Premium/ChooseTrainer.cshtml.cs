using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Premium
{
    [Authorize]
    public class ChooseTrainerModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ChooseTrainerModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerProfile> Trainers { get; set; } = new();

        public List<TrainerSubscriptionPlan> Plans { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            bool isPremiumByRole = User.IsInRole("Premium");

            bool hasApprovedRequest = await _db.PremiumRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "Approved");

            if (!isPremiumByRole && !hasApprovedRequest)
                return RedirectToPage("/Premium/Index");

            Plans = await _db.TrainerSubscriptionPlans
                .Include(p => p.TrainerProfile)
                .ThenInclude(t => t.User)
                .Where(p => p.IsActive)
                .ToListAsync();

            return Page();
        }



        public async Task<IActionResult> OnPostAsync(int trainerProfileId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (subscription == null)
                return RedirectToPage("/Premium/Index");

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.Id == trainerProfileId);

            if (trainerProfile == null)
                return NotFound();

            subscription.TrainerId = trainerProfile.UserId; // ✅ convertim profileId -> userId

            await _db.SaveChangesAsync();

            _db.Notifications.Add(new Notification
            {
                UserId = trainerProfile.UserId,
                Message = "⭐ New premium client assigned."
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostSubscribeAsync(int planId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var plan = await _db.TrainerSubscriptionPlans
                .Include(p => p.TrainerProfile)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return NotFound();

            // ✅ la tine TrainerId trebuie să fie TrainerProfileId
            var trainerProfileId = plan.TrainerProfileId;

            // (opțional) dacă există deja subscription activ, îl actualizăm în loc să adăugăm altul
            var existing = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "active");

            if (existing != null)
            {
                existing.TrainerId = trainerProfileId;
                existing.PlanType = plan.Title;
                existing.StartDate = DateTime.UtcNow;
                existing.EndDate = DateTime.UtcNow.AddDays(plan.DurationDays);
                existing.Status = "active";
            }
            else
            {
                _db.Subscriptions.Add(new Subscription
                {
                    UserId = userId,
                    TrainerId = trainerProfileId, // ✅ plan.TrainerProfileId
                    PlanType = plan.Title,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
                    Status = "active"
                });
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("/Dashboard/Index");
        }


    }
}
