using FitQuest.Data;
using FitQuest.Models;
using FitQuest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Validation
{
    [Authorize(Roles = "Trainer,Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly LevelUpService _levelUpService;
        private readonly QuestService _questService;

        public IndexModel(ApplicationDbContext db, IWebHostEnvironment env, LevelUpService levelUpService, QuestService questService)
        {
            _db = db;
            _env = env;
            _levelUpService = levelUpService;
            _questService = questService;
        }

        public List<Activity> PendingActivities { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        // ✅ new: global vs subscribers
        [BindProperty(SupportsGet = true)]
        public string Scope { get; set; } = "global"; // "global" | "subscribers"

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // 🔒 blocare trainer neaprobat
            if (User.IsInRole("Trainer"))
            {
                var trainerProfile = await _db.TrainerProfiles
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (trainerProfile == null || !trainerProfile.IsApproved)
                {
                    TempData["PendingApproval"] = true;
                    return RedirectToPage("/Trainer/Dashboard");
                }
            }

            var now = DateTime.UtcNow;

            // expirăm evidențele vechi
            var toExpire = await _db.Activities
                .Include(a => a.Evidences)
                .Where(a => a.Status == ActivityStatus.Pending
                            && a.Evidences != null
                            && a.Evidences.Any(e => !e.Validated && e.ExpiresAt < now))
                .ToListAsync();

            if (toExpire.Count > 0)
            {
                foreach (var a in toExpire)
                    a.Status = ActivityStatus.Expired;

                await _db.SaveChangesAsync();
            }

            // ✅ Query de bază (global)
            IQueryable<Activity> q = _db.Activities
                .Include(a => a.User)
                .Include(a => a.Evidences)
                .Where(a => (a.Status == ActivityStatus.Pending || a.Status == ActivityStatus.Expired)
                            && a.Evidences != null
                            && a.Evidences.Any(e => !e.Validated));

            // ✅ dacă trainer + scope=subscribers => doar abonații lui
            if (User.IsInRole("Trainer") && Scope == "subscribers")
            {
                var trainerProfile = await _db.TrainerProfiles
                    .FirstAsync(t => t.UserId == userId);

                var subscriberIds = await _db.Subscriptions
                    .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync();

                q = q.Where(a => subscriberIds.Contains(a.UserId));
            }

            PendingActivities = await q
                .OrderBy(a => a.Date)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int activityId)
        {
            var activity = await _db.Activities
                .Include(a => a.Evidences)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return NotFound();

            // ✅ securitate: dacă trainer -> voie doar în scope global SAU dacă e abonatul lui (în scope=subscribers)
            if (User.IsInRole("Trainer"))
            {
                var me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var trainerProfile = await _db.TrainerProfiles.FirstAsync(t => t.UserId == me);

                if (Scope == "subscribers")
                {
                    bool isMySubscriber = await _db.Subscriptions.AnyAsync(s =>
                        s.TrainerId == trainerProfile.Id && s.UserId == activity.UserId && s.Status == "active");

                    if (!isMySubscriber) return Forbid();
                }
                // dacă e global, îl lași cum ai zis (global validation)
            }

            var now = DateTime.UtcNow;

            bool hasExpiredEvidence = activity.Evidences != null &&
                activity.Evidences.Any(e => !e.Validated && e.ExpiresAt < now);

            if (hasExpiredEvidence)
            {
                activity.Status = ActivityStatus.Expired;
                await _db.SaveChangesAsync();

                Message = $"Activity #{activity.Id} expired (evidence expired) and cannot be approved.";
                return RedirectToPage(new { Scope });
            }

            activity.Status = ActivityStatus.Approved;

            if (activity.Evidences != null)
            {
                foreach (var e in activity.Evidences)
                    e.Validated = true;
            }

            bool alreadyGiven = await _db.XPEvents
                .AnyAsync(x => x.ActivityId == activity.Id);

            if (!alreadyGiven)
            {
                activity.User.Xp += activity.FullXp;

                _db.XPEvents.Add(new XPEvent
                {
                    UserId = activity.UserId,
                    ActivityId = activity.Id,
                    XPValue = activity.FullXp,
                    Reason = "Full XP after video validation (approved)."
                });

                activity.XpAwarded = true;
            }

            // ✅ notificare corectă doar approve
            _db.Notifications.Add(new Notification
            {
                UserId = activity.UserId,
                Message = $"✅ Your activity #{activity.Id} was approved."
            });

            await _db.SaveChangesAsync();
            await _questService.OnActivityApprovedAsync(activity.UserId, activity.Duration);
            await _levelUpService.CheckAndNotifyAsync(activity.UserId);

            DeleteEvidenceFiles(activity);

            Message = $"Activity #{activity.Id} approved (+{activity.FullXp} XP).";
            return RedirectToPage(new { Scope });
        }

        public async Task<IActionResult> OnPostRejectAsync(int activityId)
        {
            var activity = await _db.Activities
                .Include(a => a.Evidences)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null) return NotFound();

            // ✅ securitate: dacă trainer și scope=subscribers -> doar abonații lui
            if (User.IsInRole("Trainer"))
            {
                var me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var trainerProfile = await _db.TrainerProfiles.FirstAsync(t => t.UserId == me);

                if (Scope == "subscribers")
                {
                    bool isMySubscriber = await _db.Subscriptions.AnyAsync(s =>
                        s.TrainerId == trainerProfile.Id && s.UserId == activity.UserId && s.Status == "active");

                    if (!isMySubscriber) return Forbid();
                }
            }

            var now = DateTime.UtcNow;

            bool hasExpiredEvidence = activity.Evidences != null &&
                activity.Evidences.Any(e => !e.Validated && e.ExpiresAt < now);

            if (hasExpiredEvidence)
            {
                activity.Status = ActivityStatus.Expired;
                await _db.SaveChangesAsync();

                Message = $"Activity #{activity.Id} expired (evidence expired).";
                return RedirectToPage(new { Scope });
            }

            activity.Status = ActivityStatus.Rejected;

            if (activity.Evidences != null)
            {
                foreach (var e in activity.Evidences)
                    e.Validated = true;
            }

            // ✅ notificare corectă doar reject
            _db.Notifications.Add(new Notification
            {
                UserId = activity.UserId,
                Message = $"❌ Your activity #{activity.Id} was rejected."
            });

            await _db.SaveChangesAsync();

            DeleteEvidenceFiles(activity);

            Message = $"Activity #{activity.Id} rejected.";
            return RedirectToPage(new { Scope });
        }

        private void DeleteEvidenceFiles(Activity activity)
        {
            if (activity.Evidences == null) return;

            foreach (var e in activity.Evidences)
            {
                if (string.IsNullOrWhiteSpace(e.FilePath)) continue;

                var relative = e.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(_env.WebRootPath, relative);

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
        }
    }
}