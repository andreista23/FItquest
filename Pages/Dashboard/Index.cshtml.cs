using Microsoft.AspNetCore.Mvc.RazorPages;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FitQuest.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public List<Badge> MyBadges { get; set; } = new();

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

            TotalActivities = await _db.Activities
                .Where(a => a.UserId == userId)
                .CountAsync();

            LastWeekActivities = await _db.Activities
                .Where(a => a.UserId == userId && a.Date >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            TotalXP = await _db.XPEvents
                .Where(x => x.UserId == userId)
                .SumAsync(x => (int?)x.XPValue) ?? 0;

            Level = TotalXP / 100;

            if (Level >= 5 && Level % 5 == 0)
            {
                var badgeTitle = $"Level {Level}";

                var badge = await _db.Badges
                    .FirstOrDefaultAsync(b => b.Title == badgeTitle);

                if (badge != null)
                {
                    bool alreadyHasBadge = await _db.UserBadges
                        .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id);

                    if (!alreadyHasBadge)
                    {
                        _db.UserBadges.Add(new UserBadge
                        {
                            UserId = userId,
                            BadgeId = badge.Id,
                            EarnedAt = DateTime.UtcNow
                        });

                        _db.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Message = $"🏅 Ai obținut insigna \"{badge.Title}\"!"
                        });

                        await _db.SaveChangesAsync();
                    }
                }
            }

            LevelProgressPercent = TotalXP % 100;

            RecentActivities = await _db.Activities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToListAsync();

            MyBadges = await _db.UserBadges
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .OrderByDescending(ub => ub.EarnedAt)
                .Select(ub => ub.Badge)
                .ToListAsync();


            return Page();
        }
    }
}
