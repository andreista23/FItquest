using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Leaderboard
{
    [Authorize]
    public class FriendsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public FriendsModel(ApplicationDbContext db) { _db = db; }

        public List<IndexModel.LeaderboardRow> Rows { get; set; } = new();

        public async Task OnGetAsync()
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var accepted = await _db.Friendships
                .Where(f => f.Status == "Accepted" && (f.RequesterId == me || f.AddresseeId == me))
                .ToListAsync();

            var friendIds = accepted
                .Select(f => f.RequesterId == me ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();

            friendIds.Add(me);

            var xpByUser = await _db.XPEvents
                .Where(x => friendIds.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, TotalXp = g.Sum(x => x.XPValue) })
                .ToListAsync();

            var users = await _db.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            var rows = (from u in users
                        join xp in xpByUser on u.Id equals xp.UserId into xpJoin
                        from xp in xpJoin.DefaultIfEmpty()
                        select new IndexModel.LeaderboardRow
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

            Rows = rows;
        }
    }
}