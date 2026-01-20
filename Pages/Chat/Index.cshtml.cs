using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Chat
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<ChatMessage> Messages { get; set; } = new();

        [BindProperty]
        public string NewMessage { get; set; } = string.Empty;

        private int TrainerProfileId;
        private int UserId;
        private bool IsTrainer;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadContextAsync();
            await LoadMessagesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadContextAsync();

            if (TrainerProfileId == 0)
                return RedirectToPage();

            _db.ChatMessages.Add(new ChatMessage
            {
                TrainerProfileId = TrainerProfileId,
                UserId = UserId,
                Message = NewMessage,
                SentByTrainer = IsTrainer
            });

            // notificare
            int targetUserId = IsTrainer ? UserId : _db.TrainerProfiles
                .Where(t => t.Id == TrainerProfileId)
                .Select(t => t.UserId)
                .First();

            _db.Notifications.Add(new Notification
            {
                UserId = targetUserId,
                Message = "💬 Ai primit un mesaj nou."
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        private async Task LoadContextAsync()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (User.IsInRole("Trainer"))
            {
                IsTrainer = true;
                var trainerProfile = await _db.TrainerProfiles
                    .FirstAsync(t => t.UserId == currentUserId);

                TrainerProfileId = trainerProfile.Id;

                UserId = await _db.Subscriptions
                    .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                    .Select(s => s.UserId)
                    .FirstAsync(); // demo: primul abonat
            }
            else
            {
                IsTrainer = false;
                UserId = currentUserId;

                var trainerId = await _db.Subscriptions
                    .Where(s => s.UserId == currentUserId && s.Status == "active")
                    .Select(s => s.TrainerId)
                    .FirstOrDefaultAsync();

                if (trainerId == null)
                {
                    TrainerProfileId = 0;
                    return;
                }

                TrainerProfileId = trainerId.Value;
            }
        }

        private async Task LoadMessagesAsync()
        {
            if (TrainerProfileId == 0)
            {
                Messages = new List<ChatMessage>();
                return;
            }

            Messages = await _db.ChatMessages
                .Where(m => m.TrainerProfileId == TrainerProfileId && m.UserId == UserId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

    }
}
