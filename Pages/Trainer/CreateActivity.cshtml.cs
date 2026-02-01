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
        public TrainerActivity Activity { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var trainerUserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var trainerProfile = await _db.TrainerProfiles
                .FirstAsync(t => t.UserId == trainerUserId);

            Activity.TrainerProfileId = trainerProfile.Id;

            _db.TrainerActivities.Add(Activity);
            await _db.SaveChangesAsync();

            // asignăm automat tuturor abonaților
            var subscribers = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                .ToListAsync();

            foreach (var sub in subscribers)
            {
                _db.TrainerActivityAssignments.Add(
                    new TrainerActivityAssignment
                    {
                        TrainerActivityId = Activity.Id,
                        UserId = sub.UserId
                    }
                );
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("/Trainer/Dashboard");
        }
    }
}
