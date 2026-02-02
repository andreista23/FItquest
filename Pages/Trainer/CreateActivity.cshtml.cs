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
    public class CreateActivityModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CreateActivityModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public TrainerActivity Activity { get; set; } = new(); // ✅ important

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null)
                return Forbid();

            Activity.TrainerProfileId = trainerProfile.Id;

            _db.TrainerActivities.Add(Activity);
            await _db.SaveChangesAsync();

            // asignăm automat tuturor abonaților trainerului (TrainerId = TrainerProfileId ✅)
            var subscribers = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var subUserId in subscribers)
            {
                bool exists = await _db.TrainerActivityAssignments.AnyAsync(a =>
                    a.UserId == subUserId && a.TrainerActivityId == Activity.Id);

                if (!exists)
                {
                    _db.TrainerActivityAssignments.Add(new TrainerActivityAssignment
                    {
                        TrainerActivityId = Activity.Id,
                        UserId = subUserId
                    });
                }
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("/Trainer/MyActivities"); // ✅ să vezi imediat
        }
    }
}