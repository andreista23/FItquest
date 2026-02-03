using FitQuest.Data;
using FitQuest.Models;
using FitQuest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Quests
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly QuestService _questService;

        public IndexModel(ApplicationDbContext db, QuestService questService)
        {
            _db = db;
            _questService = questService;
        }

        public List<UserQuest> ActiveQuests { get; set; } = new();
        public List<UserQuest> CompletedQuests { get; set; } = new();

        public async Task OnGetAsync()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // asigură questuri (până la 3, dacă există în sistem)
            await _questService.EnsureUpToThreeActiveQuestsAsync(userId);

            ActiveQuests = await _db.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId
                             && uq.State == "active"
                             && uq.Quest.IsActive)      
                .OrderByDescending(uq => uq.AssignedAt)
                .ToListAsync();


            CompletedQuests = await _db.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId && uq.State == "completed")
                .OrderByDescending(uq => uq.CompletedAt)
                .Take(10)
                .ToListAsync();
        }

        public static int CalcPercent(int progress, int target)
        {
            if (target <= 0) return 0;
            var p = (int)Math.Round((double)progress * 100.0 / target);
            return Math.Max(0, Math.Min(100, p));
        }
    }
}
