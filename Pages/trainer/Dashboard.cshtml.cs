using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DashboardModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public int PendingActivities { get; set; }
        public int ApprovedActivities { get; set; }
        public int TotalClients { get; set; }

        public List<Activity> RecentPending { get; set; } = new();

        public async Task OnGetAsync()
        {
            PendingActivities = await _db.Activities
                .Where(a => a.Status == ActivityStatus.Pending)
                .CountAsync();

            ApprovedActivities = await _db.Activities
                .Where(a => a.Status == ActivityStatus.Approved)
                .CountAsync();

            // Mock pentru Sprint 4
            TotalClients = 5;

            RecentPending = await _db.Activities
                .Include(a => a.User)
                .Where(a => a.Status == ActivityStatus.Pending)
                .OrderBy(a => a.Date)
                .Take(5)
                .ToListAsync();
        }
    }
}
