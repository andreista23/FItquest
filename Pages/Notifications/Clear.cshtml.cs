using FitQuest.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Notifications
{
    [Authorize]
    public class ClearModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ClearModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
                return RedirectToPage("/Index");

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            if (notifications.Count > 0)
            {
                _db.Notifications.RemoveRange(notifications);
                await _db.SaveChangesAsync();
            }

            // ne întoarcem la pagina de unde a venit request-ul (nice UX)
            var referer = Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(referer))
                return Redirect(referer);

            return RedirectToPage("/Dashboard/Index");
        }
    }
}
