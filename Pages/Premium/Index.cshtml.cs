using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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


        public bool IsPremium { get; set; }
        public bool HasTrainer { get; set; }

        public string? PremiumStatus { get; set; }

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var request = await _db.PremiumRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            PremiumStatus = request?.Status;
        }



        public async Task<IActionResult> OnPostAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // mock premium activation
            user.Role = UserRole.Premium;

            _db.Subscriptions.Add(new Subscription
            {
                UserId = userId,
                TrainerId = null, // 🔥 OBLIGATORIU
                PlanType = "Premium",
                Status = "active"
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Premium/ChooseTrainer");
        }
    }
}
