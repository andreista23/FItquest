using Microsoft.AspNetCore.Mvc.RazorPages;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FitQuest.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public int TotalActivities { get; set; }
        public int LastWeekActivities { get; set; }

        public int TotalXP { get; set; }
        public int Level { get; set; }
        public int LevelProgressPercent { get; set; }

        public List<Activity> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToPage("/Account/Login");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // 1. Total activități
            TotalActivities = await _db.Activities
                .Where(a => a.UserId == userId)
                .CountAsync();

            // 2. Activități din ultima săptămână
            LastWeekActivities = await _db.Activities
                .Where(a => a.UserId == userId && a.Date >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            // 3. XP Total — din XPEvent.XPValue
            TotalXP = await _db.XPEvents
                .Where(x => x.UserId == userId)
                .SumAsync(x => (int?)x.XPValue) ?? 0;

            // 4. Calcul nivel
            Level = TotalXP / 100;
            LevelProgressPercent = TotalXP % 100;

            // 5. Activități recente
            RecentActivities = await _db.Activities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}
