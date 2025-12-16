using FitQuest.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Leaderboard
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public class LeaderboardRow
        {
            public int UserId { get; set; }
            public string Name { get; set; } = "Unknown";
            public int TotalXp { get; set; }
            public int Rank { get; set; }
        }

        public List<LeaderboardRow> TopUsers { get; set; } = new();
        public LeaderboardRow? Me { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var xpByUser = await _db.XPEvents
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, TotalXp = g.Sum(x => x.XPValue) })
                .ToListAsync();

            var users = await _db.Users
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            var rows = (from u in users
                        join xp in xpByUser on u.Id equals xp.UserId into xpJoin
                        from xp in xpJoin.DefaultIfEmpty()
                        select new LeaderboardRow
                        {
                            UserId = u.Id,
                            Name = string.IsNullOrWhiteSpace(u.Name) ? $"User {u.Id}" : u.Name,
                            TotalXp = xp?.TotalXp ?? 0
                        })
                        .OrderByDescending(r => r.TotalXp)
                        .ThenBy(r => r.UserId) 
                        .ToList();

            for (int i = 0; i < rows.Count; i++)
                rows[i].Rank = i + 1;

            TopUsers = rows.Take(20).ToList();

            Me = rows.FirstOrDefault(r => r.UserId == userId);

            return Page();
        }
    }
}
