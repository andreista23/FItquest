using System.Security.Claims;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Activity> Activities { get; set; } = new();

        public async Task OnGetAsync()
        {
            // ID-ul intern al user-ului (int) pus în ClaimTypes.NameIdentifier
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                Activities = new();
                return;
            }

            Activities = await _db.Activities
                .Where(a => a.UserId == userId)
                .Include(a => a.Evidences) // ca să putem arăta link către video
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
    }
}
