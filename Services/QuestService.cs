using FitQuest.Data;
using FitQuest.Models;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Services
{
    public class QuestService
    {
        private readonly ApplicationDbContext _db;
        private readonly LevelUpService _levelUp;

        public QuestService(ApplicationDbContext db, LevelUpService levelUp)
        {
            _db = db;
            _levelUp = levelUp;
        }

        // ✅ user primește până la 3 questuri active.
        // Dacă există doar 1-2 questuri în DB, primește doar 1-2.
        public async Task EnsureUpToThreeActiveQuestsAsync(int userId)
        {
            int activeCount = await _db.UserQuests
                .CountAsync(uq => uq.UserId == userId && uq.State == "active");

            int need = 3 - activeCount;
            if (need <= 0) return;

            // dacă nu există questuri active în sistem, nu face nimic
            var alreadyAssigned = await _db.UserQuests
                .Where(uq => uq.UserId == userId && uq.State == "active")
                .Select(uq => uq.QuestId)
                .ToListAsync();

            var candidates = await _db.Quests
                .Where(q => q.IsActive && !alreadyAssigned.Contains(q.Id))
                .OrderBy(q => Guid.NewGuid())
                .Take(need)
                .ToListAsync();

            if (candidates.Count == 0) return;

            foreach (var q in candidates)
            {
                _db.UserQuests.Add(new UserQuest
                {
                    UserId = userId,
                    QuestId = q.Id,
                    State = "active",
                    Progress = 0,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        // ✅ cheamă asta DOAR când o activitate a fost aprobată (validată)
        public async Task OnActivityApprovedAsync(int userId, int activityDurationMinutes)
        {
            await EnsureUpToThreeActiveQuestsAsync(userId);

            var active = await _db.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId && uq.State == "active")
                .ToListAsync();

            foreach (var uq in active)
            {
                switch (uq.Quest.Type)
                {
                    case QuestType.ValidatedActivitiesCount:
                        uq.Progress = Math.Min(uq.Progress + 1, uq.Quest.Target);
                        break;

                    case QuestType.ValidatedMinutesSum:
                        uq.Progress = Math.Min(uq.Progress + Math.Max(0, activityDurationMinutes), uq.Quest.Target);
                        break;

                        // Login streak îl faci separat la login, dacă vrei
                }

                await TryCompleteAsync(uq);
            }

            await _db.SaveChangesAsync();
            await EnsureUpToThreeActiveQuestsAsync(userId);
        }

        private async Task TryCompleteAsync(UserQuest uq)
        {
            if (uq.State != "active") return;
            if (uq.Progress < uq.Quest.Target) return;

            uq.State = "completed";
            uq.CompletedAt = DateTime.UtcNow;

            _db.XPEvents.Add(new XPEvent
            {
                UserId = uq.UserId,
                ActivityId = null,
                XPValue = uq.Quest.RewardXP,
                Reason = $"Quest completed: {uq.Quest.Title}"
            });

            _db.Notifications.Add(new Notification
            {
                UserId = uq.UserId,
                Message = $"✅ Quest completed: {uq.Quest.Title} (+{uq.Quest.RewardXP} XP)"
            });

            await _db.SaveChangesAsync();
            await _levelUp.CheckAndNotifyAsync(uq.UserId);
        }
    }
}
