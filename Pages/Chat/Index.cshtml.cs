using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

        // ====== DATA ======
        public List<ChatMessage> Messages { get; set; } = new();
        public List<User> ChatUsers { get; set; } = new();

        [BindProperty]
        public string NewMessage { get; set; } = string.Empty;

        public int TrainerProfileId { get; private set; }
        public int UserId { get; private set; }
        public bool IsTrainer { get; private set; }
        public int? SelectedUserId { get; private set; }

        // ====== GET ======
        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            await LoadContextAsync();

            if (IsTrainer)
            {
                await LoadTrainerUsersAsync();

                SelectedUserId = userId ?? ChatUsers.FirstOrDefault()?.Id;
                UserId = SelectedUserId ?? 0;
            }

            await LoadMessagesAsync();
            return Page();
        }

        // ====== POST (send message) ======
        public async Task<IActionResult> OnPostAsync(int? userId)
        {
            await LoadContextAsync();

            if (IsTrainer)
                UserId = userId ?? UserId;

            if (TrainerProfileId == 0 || UserId == 0)
                return RedirectToPage();

            // 1️⃣ Save message
            _db.ChatMessages.Add(new ChatMessage
            {
                TrainerProfileId = TrainerProfileId,
                UserId = UserId,
                Message = NewMessage,
                SentByTrainer = IsTrainer,
                SentAt = DateTime.UtcNow
            });

            // 2️⃣ Notification
            int targetUserId;

            if (IsTrainer)
            {
                targetUserId = UserId; // trainer -> user
            }
            else
            {
                targetUserId = await _db.TrainerProfiles
                    .Where(t => t.Id == TrainerProfileId)
                    .Select(t => t.UserId)
                    .FirstAsync(); // user -> trainer
            }

            _db.Notifications.Add(new Notification
            {
                UserId = targetUserId,
                Message = "💬 Ai primit un mesaj nou.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _db.SaveChangesAsync();

            return RedirectToPage(new { userId = UserId });
        }

        // ====== CONTEXT ======
        private async Task LoadContextAsync()
        {
            IsTrainer = User.IsInRole("Trainer");

            var lastChat = await _db.ChatMessages
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            if (lastChat == null)
            {
                TrainerProfileId = 0;
                UserId = 0;
                return;
            }

            TrainerProfileId = lastChat.TrainerProfileId;
            UserId = lastChat.UserId;
        }

        // ====== LOAD MESSAGES ======
        private async Task LoadMessagesAsync()
        {
            if (TrainerProfileId == 0 || UserId == 0)
            {
                Messages = new();
                return;
            }

            Messages = await _db.ChatMessages
                .Where(m => m.TrainerProfileId == TrainerProfileId &&
                            m.UserId == UserId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        // ====== TRAINER USERS ======
        private async Task LoadTrainerUsersAsync()
        {
            ChatUsers = await _db.Subscriptions
                .Where(s => s.TrainerId == TrainerProfileId && s.Status == "active")
                .Select(s => s.User)
                .ToListAsync();
        }
    }
}
