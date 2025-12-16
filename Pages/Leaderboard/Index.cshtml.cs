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

            // 1) calcul XP per user (din XPEvents)
            var xpByUser = await _db.XPEvents
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, TotalXp = g.Sum(x => x.XPValue) })
                .ToListAsync();

            // 2) ia userii (nume/email) + join cu XP
            //    presupun: Users are Name (sau Username). Dacă ai alt câmp, schimbă aici.
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
                        .ThenBy(r => r.UserId) // tie-break stabil
                        .ToList();

            // 3) setează rank (1..n)
            for (int i = 0; i < rows.Count; i++)
                rows[i].Rank = i + 1;

            // 4) Top N (ex: 20)
            TopUsers = rows.Take(20).ToList();

            // 5) eu + locul meu
            Me = rows.FirstOrDefault(r => r.UserId == userId);

            return Page();
        }
    }
}
