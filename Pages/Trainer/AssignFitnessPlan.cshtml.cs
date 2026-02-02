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
    public class AssignFitnessPlanModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public AssignFitnessPlanModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int PlanId { get; set; }

        public FitnessPlan? Plan { get; set; }

        public List<SelectListItem> Subscribers { get; set; } = new();

        [BindProperty]
        public int SelectedUserId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null) return Forbid();

            Plan = await _db.FitnessPlans
                .Where(p => p.Id == PlanId && p.TrainerProfileId == trainerProfile.Id)
                .Include(p => p.Items)
                    .ThenInclude(i => i.TrainerActivity)
                .FirstOrDefaultAsync();

            if (Plan == null) return NotFound();

            var subscriberUsers = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                .Select(s => s.User)
                .Distinct()
                .ToListAsync();

            Subscribers = subscriberUsers.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Name} (#{u.Id})"
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null) return Forbid();

            // planul trebuie sa fie al trainerului
            bool planIsMine = await _db.FitnessPlans
                .AnyAsync(p => p.Id == PlanId && p.TrainerProfileId == trainerProfile.Id);

            if (!planIsMine) return Forbid();

            // userul trebuie sa fie abonat la trainer
            bool isSubscriber = await _db.Subscriptions
                .AnyAsync(s =>
                    s.TrainerId == trainerProfile.Id &&
                    s.UserId == SelectedUserId &&
                    s.Status == "active");

            if (!isSubscriber) return BadRequest("User is not your active subscriber.");

            // ✅ 1) dezactivăm orice asignare activă existentă pentru acest plan (plan -> un singur user activ)
            var existing = await _db.FitnessPlanAssignments
                .Where(a => a.FitnessPlanId == PlanId && a.IsActive)
                .ToListAsync();

            foreach (var a in existing)
                a.IsActive = false;

            // ✅ 2) adăugăm asignarea nouă (mereu)
            _db.FitnessPlanAssignments.Add(new FitnessPlanAssignment
            {
                FitnessPlanId = PlanId,
                UserId = SelectedUserId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });

            // ✅ 3) notificare user
            _db.Notifications.Add(new Notification
            {
                UserId = SelectedUserId,
                Message = "📚 Ai primit un plan nou de la antrenor."
            });

            // ✅ 4) salvăm o singură dată
            await _db.SaveChangesAsync();

            return RedirectToPage("/Trainer/MyPlans");
        }
    }
}