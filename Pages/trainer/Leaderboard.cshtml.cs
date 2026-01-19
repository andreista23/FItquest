using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class LeaderboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public LeaderboardModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerRow> Trainers { get; set; } = new();

        public class TrainerRow
        {
            public string Name { get; set; } = "";
            public int ValidatedCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            Trainers = await _db.Users
                .Where(u => u.Role == UserRole.Trainer)
                .Select(u => new TrainerRow
                {
                    Name = u.Name,
                    ValidatedCount = _db.Activities
                        .Count(a => a.Status == ActivityStatus.Approved)
                })
                .OrderByDescending(t => t.ValidatedCount)
                .ToListAsync();
        }
    }
}
