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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) { _db = db; }

        public List<User> Friends { get; set; } = new();
        public List<Friendship> IncomingRequests { get; set; } = new();

        [BindProperty]
        public string? Search { get; set; }

        public List<User> SearchResults { get; set; } = new();

        public async Task OnGetAsync()
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // prieteni accepted
            var accepted = await _db.Friendships
                .Where(f => f.Status == "Accepted" && (f.RequesterId == me || f.AddresseeId == me))
                .ToListAsync();

            var friendIds = accepted
                .Select(f => f.RequesterId == me ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();

            Friends = await _db.Users
                .Where(u => friendIds.Contains(u.Id))
                .ToListAsync();

            // requests primite
            IncomingRequests = await _db.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == me && f.Status == "Pending")
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            await OnGetAsync();

            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (string.IsNullOrWhiteSpace(Search))
                return Page();

            var q = Search.Trim().ToLower();

            SearchResults = await _db.Users
                .Where(u => u.Id != me &&
                            (u.Name.ToLower().Contains(q) || u.Email.ToLower().Contains(q)))
                .Take(20)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSendRequestAsync(int toUserId)
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (toUserId == me) return RedirectToPage();

            // blocăm duplicate în ambele sensuri
            bool exists = await _db.Friendships.AnyAsync(f =>
                (f.RequesterId == me && f.AddresseeId == toUserId) ||
                (f.RequesterId == toUserId && f.AddresseeId == me));

            if (!exists)
            {
                _db.Friendships.Add(new Friendship
                {
                    RequesterId = me,
                    AddresseeId = toUserId,
                    Status = "Pending"
                });

                _db.Notifications.Add(new Notification
                {
                    UserId = toUserId,
                    Message = $"🤝 Ai primit o cerere de prietenie."
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAcceptAsync(int friendshipId)
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var f = await _db.Friendships.FirstOrDefaultAsync(x =>
                x.Id == friendshipId && x.AddresseeId == me && x.Status == "Pending");

            if (f == null) return RedirectToPage();

            f.Status = "Accepted";
            f.RespondedAt = DateTime.UtcNow;

            _db.Notifications.Add(new Notification
            {
                UserId = f.RequesterId,
                Message = "✅ Cererea ta de prietenie a fost acceptată."
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeclineAsync(int friendshipId)
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var f = await _db.Friendships.FirstOrDefaultAsync(x =>
                x.Id == friendshipId && x.AddresseeId == me && x.Status == "Pending");

            if (f == null) return RedirectToPage();

            f.Status = "Declined";
            f.RespondedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}