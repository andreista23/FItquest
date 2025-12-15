using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace FitQuest.Pages.Validation
{
    // PROVIZORIU:
    // [Authorize] sau [Authorize(Roles="Trainer")] când implementezi rolurile corect
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public IndexModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public List<Activity> PendingActivities { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            // Luăm activitățile Pending care au cel puțin un Evidence nevalidat
            PendingActivities = await _db.Activities
                .Include(a => a.User)
                .Include(a => a.Evidences)
                .Where(a => a.Status == ActivityStatus.Pending
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

            // 1️⃣ setăm status Approved
            activity.Status = ActivityStatus.Approved;

            // 2️⃣ marcăm evidence-urile ca validate
            if (activity.Evidences != null)
            {
                foreach (var e in activity.Evidences)
                    e.Validated = true;
            }

            // 3️⃣ ACORDARE XP FULL (doar o singură dată)
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

            // 4️⃣ ștergere fișiere video (privacy)
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

            activity.Status = ActivityStatus.Rejected;

            if (activity.Evidences != null)
            {
                foreach (var e in activity.Evidences)
                    e.Validated = true; // “am luat o decizie”, deci nu mai e în coada de validare
            }

            await _db.SaveChangesAsync();

            // opțional: ștergere fișiere după respingere
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

                // FilePath e de forma "/evidence/xxx.mp4"
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
