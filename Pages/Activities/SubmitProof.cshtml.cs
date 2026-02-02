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

            // allow: doar mesaj fără fișier (dacă vrei obligatoriu fișier, atunci verifică ProofFile != null)
            if (ProofFile != null && ProofFile.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "trainer_proofs");
                Directory.CreateDirectory(folder);

                var ext = Path.GetExtension(ProofFile.FileName);
                var safeName = $"{userId}_{assignment.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
                var relativePath = Path.Combine("trainer_proofs", safeName).Replace("\\", "/");
                var fullPath = Path.Combine(_env.WebRootPath, relativePath);

                using (var fs = new FileStream(fullPath, FileMode.Create))
                {
                    await ProofFile.CopyToAsync(fs);
                }

                assignment.ProofPath = relativePath;
            }

            assignment.ProofMessage = string.IsNullOrWhiteSpace(ProofMessage) ? null : ProofMessage.Trim();
            assignment.IsCompleted = true;
            assignment.CompletedAt = DateTime.UtcNow;

            // 🔔 notificare trainer
            // TrainerProfile are (aproape sigur) UserId; dacă la tine e alt nume, schimbă linia
            var trainerUserId = assignment.TrainerActivity.TrainerProfile.UserId;

            _db.Notifications.Add(new Notification
            {
                UserId = trainerUserId,
                Message = $"✅ Client #{userId} a trimis dovadă pentru activitatea: {assignment.TrainerActivity.Title}"
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Activities/FromTrainer");
        }
    }
}