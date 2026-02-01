using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class GiveFeedbackModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public GiveFeedbackModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<User> Subscribers { get; set; } = new();

        [BindProperty]
        public int SelectedUserId { get; set; }

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            int trainerUserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var trainerProfile = await _db.TrainerProfiles
                .FirstAsync(t => t.UserId == trainerUserId);

            // Luăm userii abonați la acest trainer
            Subscribers = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                .Include(s => s.User)
                .Select(s => s.User)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedUserId == 0 || string.IsNullOrWhiteSpace(Message))
            {
                await OnGetAsync();
                ModelState.AddModelError("", "Selectează un utilizator și scrie un mesaj.");
                return Page();
            }

            int trainerUserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var trainerProfile = await _db.TrainerProfiles
                .FirstAsync(t => t.UserId == trainerUserId);

            _db.TrainerFeedbacks.Add(new TrainerFeedback
            {
                TrainerProfileId = trainerProfile.Id,
                UserId = SelectedUserId,
                Message = Message
            });

            _db.Notifications.Add(new Notification
            {
                UserId = SelectedUserId,
                Message = "📝 Ai primit feedback de la antrenor."
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Trainer/GiveFeedback");
        }
    }
}
