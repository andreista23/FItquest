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
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                Activities = new();
                return;
            }

            Activities = await _db.Activities
                .Where(a => a.UserId == userId)
                .Include(a => a.Evidences)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
    }
}
