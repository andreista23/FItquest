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
        public List<User> ChatUsers { get; set; } = new();

        [BindProperty]
        public string NewMessage { get; set; } = string.Empty;

        public int? TrainerProfileId { get; private set; }
        public int UserId { get; private set; }
        public bool IsTrainer { get; private set; }

        // userId = client selectat (doar pt trainer)
        public int? SelectedUserId { get; private set; }

        public string? InfoMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            await LoadContextAsync(userId);

            if (!IsTrainer && TrainerProfileId == null)
            {
                InfoMessage = "Trebuie să fii abonat la un antrenor ca să folosești chatul.";
                return Page();
            }


            if (TrainerProfileId == 0 || UserId == 0)
                return Page();

            await LoadMessagesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? userId)
        {
            if (string.IsNullOrWhiteSpace(NewMessage))
                return RedirectToPage(new { userId });

            await LoadContextAsync(userId);

            if (TrainerProfileId == 0 || UserId == 0)
                return RedirectToPage();

            if (TrainerProfileId == null)
                return Forbid();

            bool allowed = await IsAllowedConversationAsync(
                TrainerProfileId.Value,
                UserId
            );

            if (!allowed)
                return Forbid();

            if (!IsTrainer && TrainerProfileId == null)
            {
                TempData["ChatInfo"] = "Trebuie să fii abonat la un antrenor ca să folosești chatul.";
                return RedirectToPage();
            }


            _db.ChatMessages.Add(new ChatMessage
            {
                TrainerProfileId = TrainerProfileId.Value,
                UserId = UserId,
                Message = NewMessage.Trim(),
                SentByTrainer = IsTrainer,
                SentAt = DateTime.UtcNow
            });

            // notificare către partea cealaltă
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
                    .FirstAsync(); // user -> trainer(userId)
            }

            _db.Notifications.Add(new Notification
            {
                UserId = targetUserId,
                Message = "💬 Ai primit un mesaj nou.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _db.SaveChangesAsync();

            return RedirectToPage(new { userId = IsTrainer ? UserId : (int?)null });
        }

        private async Task LoadContextAsync(int? userIdFromRoute)
        {
            IsTrainer = User.IsInRole("Trainer");
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (IsTrainer)
            {
                // trainer logat -> TrainerProfileId al lui
                TrainerProfileId = await _db.TrainerProfiles
                    .Where(tp => tp.UserId == currentUserId)
                    .Select(tp => tp.Id)
                    .FirstOrDefaultAsync();

                if (TrainerProfileId == 0)
                {
                    UserId = 0;
                    return;
                }

                // lista clienților lui (doar abonamente active)
                ChatUsers = await _db.Subscriptions
                    .Include(s => s.User)
                    .Where(s => s.TrainerId == TrainerProfileId && s.Status == "active")
                    .Select(s => s.User)
                    .Distinct()
                    .OrderBy(u => u.Name)
                    .ToListAsync();

                SelectedUserId = userIdFromRoute ?? ChatUsers.FirstOrDefault()?.Id;
                UserId = SelectedUserId ?? 0;
                return;
            }

            // user normal -> trainer din abonamentul lui activ
            TrainerProfileId = await _db.Subscriptions
                .Where(s => s.UserId == currentUserId
                            && s.Status == "active"
                            && s.TrainerId != null)
                .OrderByDescending(s => s.Id) // sau CreatedAt dacă ai
                .Select(s => s.TrainerId)
                .FirstOrDefaultAsync();


            UserId = currentUserId;
        }

        private async Task LoadMessagesAsync()
        {
            Messages = await _db.ChatMessages
                .Where(m => m.TrainerProfileId == TrainerProfileId && m.UserId == UserId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        private async Task<bool> IsAllowedConversationAsync(int trainerProfileId, int userId)
        {
            // verificăm că există abonament activ user->trainer
            return await _db.Subscriptions.AnyAsync(s =>
                s.TrainerId == trainerProfileId &&
                s.UserId == userId &&
                s.Status == "active");
        }
    }
}
