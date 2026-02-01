using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ActivitiesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ActivitiesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Activity> Activities { get; set; } = new();

        public async Task OnGetAsync()
        {
            Activities = await _db.Activities
                .Include(a => a.User)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostInvalidateAsync(int activityId)
        {
            var activity = await _db.Activities.FindAsync(activityId);
            if (activity == null) return NotFound();

            activity.Status = ActivityStatus.Rejected;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                Action = "Invalidated activity",
                Target = $"Activity #{activity.Id}"
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int activityId)
        {
            var activity = await _db.Activities.FindAsync(activityId);
            if (activity == null) return NotFound();

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                Action = "Deleted activity",
                Target = $"Activity #{activity.Id}"
            });

            _db.Activities.Remove(activity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
