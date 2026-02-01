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

            subscription.TrainerId = trainerProfileId;

            await _db.SaveChangesAsync();

            // 🔔 notificare trainer
            var trainer = await _db.TrainerProfiles
                .Include(t => t.User)
                .FirstAsync(t => t.Id == trainerProfileId);

            _db.Notifications.Add(new Notification
            {
                UserId = trainer.UserId,
                Message = $"⭐ New premium client assigned."
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostSubscribeAsync(int planId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var plan = await _db.TrainerSubscriptionPlans.FindAsync(planId);

            _db.Subscriptions.Add(new Subscription
            {
                UserId = userId,
                TrainerId = plan.TrainerProfileId,
                PlanType = plan.Title,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
                Status = "active"
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Dashboard/Index");
        }


    }
}
