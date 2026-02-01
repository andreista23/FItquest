using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Admin
{
    [Authorize(Policy = "AdminWithGate")] // <-- IMPORTANT: gate + admin
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsersModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<User> Users { get; set; } = new();

        private int CurrentAdminId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<User?> GetTargetUserAsync(int userId)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        private static bool IsAdmin(User u) => u.Role == UserRole.Admin;

        private bool IsSelf(User u) => u.Id == CurrentAdminId;

        private IActionResult Block(string msg)
        {
            TempData["Error"] = msg;
            return RedirectToPage();
        }

        public async Task OnGetAsync()
        {
            Users = await _db.Users
                .OrderBy(u => u.Email)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostBanAsync(int userId)
        {
            var target = await GetTargetUserAsync(userId);
            if (target == null) return NotFound();

            // ❌ nu ai voie pe tine
            if (IsSelf(target))
                return Block("Nu îți poți da ban singur.");

            // ❌ nu ai voie pe alt admin
            if (IsAdmin(target))
                return Block("Nu poți da ban unui alt admin.");

            target.IsBanned = true;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = CurrentAdminId,
                Action = "Banned user",
                Target = target.Email
            });

            _db.Notifications.Add(new Notification
            {
                UserId = target.Id,
                Message = "⛔ Your account has been banned by an administrator."
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnbanAsync(int userId)
        {
            var target = await GetTargetUserAsync(userId);
            if (target == null) return NotFound();

            // ❌ nu ai voie pe tine (unban pe tine e ciudat, dar îl blocăm)
            if (IsSelf(target))
                return Block("Nu poți modifica starea de ban pentru propriul cont.");

            // ❌ nu ai voie pe alt admin
            if (IsAdmin(target))
                return Block("Nu poți da unban unui alt admin.");

            target.IsBanned = false;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = CurrentAdminId,
                Action = "Unbanned user",
                Target = target.Email
            });

            _db.Notifications.Add(new Notification
            {
                UserId = target.Id,
                Message = "✅ Your account has been unbanned."
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            var target = await GetTargetUserAsync(userId);
            if (target == null) return NotFound();

            // ❌ nu ai voie să te ștergi singur
            if (IsSelf(target))
                return Block("Nu îți poți șterge propriul cont de admin.");

            // ❌ nu ai voie să ștergi alt admin
            if (IsAdmin(target))
                return Block("Nu poți șterge un alt admin.");

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = CurrentAdminId,
                Action = "Deleted user",
                Target = target.Email
            });

            _db.Notifications.Add(new Notification
            {
                UserId = target.Id,
                Message = "❌ Your account has been deleted."
            });

            _db.Users.Remove(target);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(int userId, string newRole)
        {
            var target = await _db.Users
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (target == null)
                return NotFound();

            if (!Enum.TryParse<UserRole>(newRole, out var role))
                return BadRequest();

            // ❌ nu ai voie să schimbi rolul unui admin (nici al tău, nici al altuia)
            if (IsAdmin(target))
            {
                if (IsSelf(target))
                    return Block("Nu îți poți elimina sau modifica rolul de Admin.");
                return Block("Nu poți modifica rolul unui alt Admin.");
            }

            // dacă user e Trainer și nu mai e Trainer → ștergem profile
            if (target.Role == UserRole.Trainer && role != UserRole.Trainer)
            {
                var profile = await _db.TrainerProfiles
                    .FirstOrDefaultAsync(t => t.UserId == target.Id);

                if (profile != null)
                    _db.TrainerProfiles.Remove(profile);
            }

            target.Role = role;

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = CurrentAdminId,
                Action = "Changed user role",
                Target = $"{target.Email} → {role}"
            });

            _db.Notifications.Add(new Notification
            {
                UserId = target.Id,
                Message = $"🔁 Your role has been changed to: {role}"
            });

            await _db.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}