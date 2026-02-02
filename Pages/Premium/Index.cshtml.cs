using FitQuest.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Premium
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public string? PremiumStatus { get; set; }

        // ✅ pentru box-ul cu antrenorul
        public string? TrainerName { get; set; }
        public string? PlanTypeActive { get; set; }
        public DateTime? EndDateActive { get; set; }

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Status cerere premium
            var request = await _db.PremiumRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            PremiumStatus = request?.Status;

            // Subscription activ + trainer (TrainerId = TrainerProfileId la tine)
            var sub = await _db.Subscriptions
                .Include(s => s.Trainer)
                    .ThenInclude(t => t.User)
                .Where(s => s.UserId == userId && s.Status == "active" && s.TrainerId != null)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (sub?.Trainer != null)
            {
                TrainerName = sub.Trainer.User.Name;
                PlanTypeActive = sub.PlanType;
                EndDateActive = sub.EndDate;
            }
        }
    }
}