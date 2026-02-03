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

        //  user primește până la 3 questuri active.
        // Dacă există doar 1-2 questuri în DB, primește doar 1-2.
        public async Task EnsureUpToThreeActiveQuestsAsync(int userId)
        {
            //  numărăm doar questurile ACTIVE (UserQuest active + Quest.IsActive)
            int activeCount = await _db.UserQuests
                .Include(uq => uq.Quest)
                .CountAsync(uq => uq.UserId == userId
                                  && uq.State == "active"
                                  && uq.Quest.IsActive);

            int need = 3 - activeCount;
            if (need <= 0) return;

            // excludem toate questurile deja asignate (active/completed), ca să evităm duplicate
            var alreadyAssigned = await _db.UserQuests
                .Where(uq => uq.UserId == userId)
                .Select(uq => uq.QuestId)
                .ToListAsync();

            // candidați doar questuri ACTIVE
            var candidates = await _db.Quests
                .Where(q => q.IsActive && !alreadyAssigned.Contains(q.Id))
                .ToListAsync();

            candidates = candidates
                .OrderBy(_ => Guid.NewGuid())
                .Take(need)
                .ToList();

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


        // cheamă asta DOAR când o activitate a fost aprobată (validată)
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

        public async Task OnUserLoggedInAsync(int userId)
        {
            //asigură până la 3 questuri ACTIVE
            await EnsureUpToThreeActiveQuestsAsync(userId);

            var today = DateTime.UtcNow.Date;

            // 1) logăm azi (o singură dată)
            bool exists = await _db.UserLoginDays
                .AnyAsync(x => x.UserId == userId && x.DayUtc == today);

            if (!exists)
            {
                _db.UserLoginDays.Add(new UserLoginDay
                {
                    UserId = userId,
                    DayUtc = today
                });

                await _db.SaveChangesAsync();
            }

            // 2) calcul streak
            int streak = await CalculateStreakDaysAsync(userId, today);

            // 3) update questuri login streak
            var loginQuests = await _db.UserQuests
                .Include(uq => uq.Quest)
                .Where(uq => uq.UserId == userId
                             && uq.State == "active"
                             && uq.Quest.IsActive
                             && uq.Quest.Type == QuestType.LoginStreakDays)
                .ToListAsync();

            foreach (var uq in loginQuests)
            {
                uq.Progress = Math.Min(streak, uq.Quest.Target);
                await TryCompleteAsync(uq);
            }

            await _db.SaveChangesAsync();

            // reumple până la 3 dacă s-a completat ceva
            await EnsureUpToThreeActiveQuestsAsync(userId);
        }

        private async Task<int> CalculateStreakDaysAsync(int userId, DateTime todayUtcDate)
        {
            var days = await _db.UserLoginDays
                .Where(x => x.UserId == userId)
                .Select(x => x.DayUtc)
                .ToListAsync();

            var set = days.Select(d => d.Date).ToHashSet();

            if (!set.Contains(todayUtcDate)) return 0;

            int streak = 0;
            var cursor = todayUtcDate;

            while (set.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }

            return streak;
        }


    }
}
