using FitQuest.Data;
using FitQuest.Models;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Services
{
    public class LevelUpService
    {
        private readonly ApplicationDbContext _db;

        public LevelUpService(ApplicationDbContext db)
        {
            _db = db;
        }

        private async Task GrantMissingLevelBadgesAsync(int userId, int currentLevel)
        {
            if (currentLevel < 5) return;

            var ownedBadgeIds = await _db.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            for (int lvl = 5; lvl <= currentLevel; lvl += 5)
            {
                var badgeTitle = $"Level {lvl}";

                var badge = await _db.Badges.FirstOrDefaultAsync(b => b.Title == badgeTitle);
                if (badge == null) continue; // nu există în tabela Badges

                if (ownedBadgeIds.Contains(badge.Id)) continue;

                _db.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    EarnedAt = DateTime.UtcNow
                });

                _db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Message = $"🏅 Ai obținut insigna \"{badge.Title}\"!"
                });

                ownedBadgeIds.Add(badge.Id);
            }
        }
        public async Task CheckAndNotifyAsync(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            int totalXp = await _db.XPEvents
                .Where(x => x.UserId == userId)
                .SumAsync(x => (int?)x.XPValue) ?? 0;

            int currentLevel = totalXp / 100;
            int lastNotified = user.LastNotifiedLevel;

            // ✅ Rulează mereu: acordă badge-uri lipsă până la nivelul curent
            await GrantMissingLevelBadgesAsync(userId, currentLevel);

            // ✅ Notificări doar dacă a crescut nivelul
            if (currentLevel > lastNotified)
            {
                for (int lvl = lastNotified + 1; lvl <= currentLevel; lvl++)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Message = $"🎉 Felicitări! Ai ajuns la nivelul {lvl}!"
                    });
                }

                user.LastNotifiedLevel = currentLevel;
            }

            await _db.SaveChangesAsync();
        }
    }
    }
