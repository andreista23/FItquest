using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FitQuest.Data;
using FitQuest.Models;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class MyActivitiesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public MyActivitiesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerActivity> Activities { get; set; } = new();

        public async Task OnGetAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstAsync(t => t.UserId == trainerUserId);

            Activities = await _db.TrainerActivities
                .Where(a => a.TrainerProfileId == trainerProfile.Id)
                .Include(a => a.Assignments)
                .ToListAsync();
        }
    }
}
