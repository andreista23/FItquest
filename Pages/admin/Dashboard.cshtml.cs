using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DashboardModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public int TotalUsers { get; set; }
        public int TotalTrainers { get; set; }
        public int PendingTrainers { get; set; }

        public List<AdminLog> RecentLogs { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalUsers = await _db.Users.CountAsync();

            TotalTrainers = await _db.Users
                .CountAsync(u => u.Role == UserRole.Trainer);

            PendingTrainers = await _db.TrainerProfiles
                .CountAsync(t => !t.IsApproved);

            RecentLogs = await _db.AdminLogs
                .Include(l => l.Admin)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();
        }
    }
}
