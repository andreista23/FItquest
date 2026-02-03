using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class SubmitProofModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public SubmitProofModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty(SupportsGet = true)]
        public int AssignmentId { get; set; }

        public TrainerActivityAssignment? Assignment { get; set; }

        [BindProperty]
        public IFormFile? ProofFile { get; set; }

        [BindProperty]
        public string? ProofMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            Assignment = await _db.TrainerActivityAssignments
                .Include(a => a.TrainerActivity)
                    .ThenInclude(t => t.TrainerProfile)
                .FirstOrDefaultAsync(a => a.Id == AssignmentId && a.UserId == userId);

            if (Assignment == null) return NotFound();

            return Page();
        }
           public async Task<IActionResult> OnPostAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var assignment = await _db.TrainerActivityAssignments
                .Include(a => a.TrainerActivity)
                    .ThenInclude(t => t.TrainerProfile)
                .FirstOrDefaultAsync(a => a.Id == AssignmentId && a.UserId == userId);

            if (assignment == null) return NotFound();

            // ✅ obligatoriu VIDEO
            if (ProofFile == null || ProofFile.Length == 0)
            {
                ModelState.AddModelError("ProofFile", "Trebuie să încarci un video.");
                Assignment = assignment;
                return Page();
            }

            if (!ProofFile.ContentType.StartsWith("video/"))
            {
                ModelState.AddModelError("ProofFile", "Fișierul trebuie să fie video.");
                Assignment = assignment;
                return Page();
            }

            long maxSizeBytes = 30L * 1024L * 1024L; // 30 MB
            if (ProofFile.Length > maxSizeBytes)
            {
                ModelState.AddModelError("ProofFile", "Fișierul nu poate depăși 30 MB.");
                Assignment = assignment;
                return Page();
            }

            // ✅ 1) creăm Activity pending (trainer-assigned)
            int fullXp = CalculateFullXp(assignment.TrainerActivity); // vezi funcția mai jos

            var activity = new Activity
            {
                UserId = userId,
                Type = $"Trainer: {assignment.TrainerActivity.Title}",
                Duration = 30, // dacă nu ai duration la TrainerActivity, poți pune default sau adaugi câmp
                Date = DateTime.UtcNow,
                Status = ActivityStatus.Pending,
                FullXp = fullXp,
                XpAwarded = false,
                IsTrainerAssigned = true
            };

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync();

            // ✅ 2) salvăm video ca Evidence (folosim același folder ca la global, sau altul)
            var folder = Path.Combine(_env.WebRootPath, "evidence");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(ProofFile.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var physicalPath = Path.Combine(folder, fileName);

            using (var fs = new FileStream(physicalPath, FileMode.Create))
                await ProofFile.CopyToAsync(fs);

            var relativePath = $"/evidence/{fileName}";

            _db.Evidence.Add(new Evidence
            {
                ActivityId = activity.Id,
                FilePath = relativePath,
                UploadedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Validated = false
            });

            // ✅ 3) legăm assignment de Activity + setăm status “submitted”
            assignment.ActivityId = activity.Id;
            assignment.ProofMessage = string.IsNullOrWhiteSpace(ProofMessage) ? null : ProofMessage.Trim();
            assignment.IsCompleted = true;
            assignment.CompletedAt = DateTime.UtcNow;

            // 🔔 notificare trainer
            var trainerUserId = assignment.TrainerActivity.TrainerProfile.UserId;
            _db.Notifications.Add(new Notification
            {
                UserId = trainerUserId,
                Message = $"🎥 Client #{userId} a trimis VIDEO pentru: {assignment.TrainerActivity.Title}"
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Activities/FromTrainer");
        }

        private static int CalculateFullXp(TrainerActivity ta)
        {
            // simplu: 100 + random bonus, poți ajusta
            int baseXp = 120;
            int bonus = Random.Shared.Next(0, 41); // 0..40
            return Math.Clamp(baseXp + bonus, 80, 300);
        }
    }
    }