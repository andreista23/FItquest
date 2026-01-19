using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TrainerApprovalsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public TrainerApprovalsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerProfile> Pending { get; set; } = new();

        public async Task OnGetAsync()
        {
            Pending = await _db.TrainerProfiles
                .Include(t => t.User)
                .Where(t => !t.IsApproved)
                .ToListAsync();
        }



        public async Task<IActionResult> OnPostApproveAsync(int trainerId)
        {
            var trainer = await _db.TrainerProfiles
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == trainerId);

            if (trainer == null)
                return NotFound();

            trainer.IsApproved = true;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Action = "Approved trainer",
                Target = trainer.User.Email
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDenyAsync(int trainerId)
        {
            var trainer = await _db.TrainerProfiles
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == trainerId);

            if (trainer == null)
                return NotFound();

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Action = "Denied trainer",
                Target = trainer.User.Email
            });

            _db.TrainerProfiles.Remove(trainer);

            _db.Notifications.Add(new Notification
            {
                UserId = trainer.UserId,
                Message = "✅ Your trainer account has been approved."
            });

            _db.Notifications.Add(new Notification
            {
                UserId = trainer.UserId,
                Message = "❌ Your trainer application was rejected."
            });



            await _db.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
