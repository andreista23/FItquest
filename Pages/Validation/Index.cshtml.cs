using FitQuest.Data;
using FitQuest.Models;
using FitQuest.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitQuest.Services;

namespace FitQuest.Pages.Validation
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly LevelUpService _levelUpService;
        public IndexModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
            _levelUpService = new LevelUpService(db);
        }

        public List<Activity> PendingActivities { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            var now = DateTime.UtcNow;

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

           
            PendingActivities = await _db.Activities
                .Include(a => a.User)
                .Include(a => a.Evidences)
                .Where(a => (a.Status == ActivityStatus.Pending || a.Status == ActivityStatus.Expired)
                            && a.Evidences != null
                            && a.Evidences.Any(e => !e.Validated))
                .OrderBy(a => a.Date)
                .ToListAsync();
        }


        public async Task<IActionResult> OnPostApproveAsync(int activityId)
        {
            var activity = await _db.Activities
                .Include(a => a.Evidences)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null)
                return NotFound();

            var now = DateTime.UtcNow;

            bool hasExpiredEvidence = activity.Evidences != null &&
                activity.Evidences.Any(e => !e.Validated && e.ExpiresAt < now);

            if (hasExpiredEvidence)
            {
                activity.Status = ActivityStatus.Expired;
                await _db.SaveChangesAsync();

                Message = $"Activity #{activity.Id} expired (evidence expired) and cannot be approved.";
                return RedirectToPage();
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

            await _db.SaveChangesAsync();
            await _levelUpService.CheckAndNotifyAsync(activity.UserId);

            DeleteEvidenceFiles(activity);

            Message = $"Activity #{activity.Id} approved (+{activity.FullXp} XP).";
            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostRejectAsync(int activityId)
        {
            var activity = await _db.Activities
                .Include(a => a.Evidences)
                .FirstOrDefaultAsync(a => a.Id == activityId);

            if (activity == null) return NotFound();

            var now = DateTime.UtcNow;

            bool hasExpiredEvidence = activity.Evidences != null &&
                activity.Evidences.Any(e => !e.Validated && e.ExpiresAt < now);

            if (hasExpiredEvidence)
            {
                activity.Status = ActivityStatus.Expired;
                await _db.SaveChangesAsync();

                Message = $"Activity #{activity.Id} expired (evidence expired).";
                return RedirectToPage();
            }


            activity.Status = ActivityStatus.Rejected;

            if (activity.Evidences != null)
            {
                foreach (var e in activity.Evidences)
                    e.Validated = true; 
            }

            await _db.SaveChangesAsync();

            DeleteEvidenceFiles(activity);

            Message = $"Activity #{activity.Id} rejected.";
            return RedirectToPage();
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
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
    }
}
