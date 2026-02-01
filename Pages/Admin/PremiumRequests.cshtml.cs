using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PremiumRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public PremiumRequestsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<PremiumRequest> Requests { get; set; } = new();

        public async Task OnGetAsync()
        {
            Requests = await _db.PremiumRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var req = await _db.PremiumRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                return NotFound();

            req.Status = "Approved";
            req.User.Role = UserRole.Premium;

            _db.Subscriptions.Add(new Subscription
            {
                UserId = req.UserId,
                TrainerId = null,
                PlanType = "Premium",
                Status = "active"
            });

            _db.Notifications.Add(new Notification
            {
                UserId = req.UserId,
                Message = "⭐ Your Premium request has been approved."
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var req = await _db.PremiumRequests.FindAsync(id);

            if (req == null)
                return NotFound();

            req.Status = "Rejected";
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
