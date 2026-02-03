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
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstAsync(t => t.UserId == trainerUserId);

            Activity.TrainerProfileId = trainerProfile.Id;

            _db.TrainerActivities.Add(Activity);
            await _db.SaveChangesAsync();

            // ✅ NU mai asignăm automat la toți
            return RedirectToPage("/Trainer/MyActivities");
        }
    }
}