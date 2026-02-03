using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class MyAssignedPlansModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public MyAssignedPlansModel(ApplicationDbContext db) { _db = db; }

        public List<FitnessPlanAssignment> AssignedPlans { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            AssignedPlans = await _db.FitnessPlanAssignments
                .Where(a => a.UserId == userId && a.IsActive) // ✅ DOAR pt userul curent
                .Include(a => a.FitnessPlan)
                    .ThenInclude(p => p.Items)
                        .ThenInclude(i => i.TrainerActivity)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }
    }
}