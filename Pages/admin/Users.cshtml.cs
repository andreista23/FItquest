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
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsersModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<User> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _db.Users
                .OrderBy(u => u.Email)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostBanAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.IsBanned = true;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Action = "Banned user",
                Target = user.Email
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnbanAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.IsBanned = false;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Action = "Unbanned user",
                Target = user.Email
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                Action = "Deleted user",
                Target = user.Email
            });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(int userId, string newRole)
        {
            var user = await _db.Users
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            if (!Enum.TryParse<UserRole>(newRole, out var role))
                return BadRequest();

            // revocare Trainer
            if (user.Role == UserRole.Trainer && role != UserRole.Trainer)
            {
                var profile = await _db.TrainerProfiles
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                if (profile != null)
                    _db.TrainerProfiles.Remove(profile);
            }

            _db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = "⛔ Your account has been banned by an administrator."
            });

            _db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = "❌ Your account has been deleted."
            });


            user.Role = role;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
                Action = "Changed user role",
                Target = $"{user.Email} → {role}"
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }


    }
}
