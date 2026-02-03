using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Friends
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DashboardModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // userul al cărui dashboard îl vezi
        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        public string ViewedUserName { get; set; } = "Friend";

        public int TotalActivities { get; set; }
        public int LastWeekActivities { get; set; }

        public int TotalXP { get; set; }
        public int Level { get; set; }
        public int LevelProgressPercent { get; set; }

        public List<Activity> RecentActivities { get; set; } = new();
        public List<Badge> MyBadges { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (UserId <= 0) return NotFound();
            if (UserId == me) return RedirectToPage("/Dashboard/Index");

            // ✅ verificăm prietenia (Accepted) în ambele sensuri
            bool areFriends = await _db.Friendships.AnyAsync(f =>
                f.Status == "Accepted" &&
                ((f.RequesterId == me && f.AddresseeId == UserId) ||
                 (f.RequesterId == UserId && f.AddresseeId == me)));

            if (!areFriends) return Forbid();

            var viewedUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (viewedUser == null) return NotFound();

            ViewedUserName = viewedUser.Name;

            // ✅ aceleași calcule ca dashboard-ul normal, dar pentru UserId (prieten)
            TotalActivities = await _db.Activities
                .Where(a => a.UserId == UserId)
                .CountAsync();

            LastWeekActivities = await _db.Activities
                .Where(a => a.UserId == UserId && a.Date >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            TotalXP = await _db.XPEvents
                .Where(x => x.UserId == UserId)
                .SumAsync(x => (int?)x.XPValue) ?? 0;

            Level = TotalXP / 100;
            LevelProgressPercent = TotalXP % 100;

            RecentActivities = await _db.Activities
                .Where(a => a.UserId == UserId)
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToListAsync();

            MyBadges = await _db.UserBadges
                .Where(ub => ub.UserId == UserId)
                .Include(ub => ub.Badge)
                .OrderByDescending(ub => ub.EarnedAt)
                .Select(ub => ub.Badge)
                .ToListAsync();

            return Page();
        }
    }
}