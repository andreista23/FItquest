using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class FromTrainerModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public FromTrainerModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerActivityAssignment> Activities { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            Activities = await _db.TrainerActivityAssignments
                .Include(a => a.TrainerActivity)
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }
    }
}
