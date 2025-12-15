using System.Security.Claims;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Activity> Activities { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                Activities = new();
                return;
            }

            // 🔔 Notificări pentru navbar
            try
            {
                ViewData["Notifications"] = await _db.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewData["UnreadNotifications"] = await _db.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch
            {
                ViewData["Notifications"] = new List<FitQuest.Models.Notification>();
                ViewData["UnreadNotifications"] = 0;
            }
        }
    }
}
