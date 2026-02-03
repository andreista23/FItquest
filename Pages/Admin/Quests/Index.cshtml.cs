using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Admin.Quests
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Quest> Quests { get; set; } = new();

        public async Task OnGetAsync()
        {
            Quests = await _db.Quests
                .OrderByDescending(q => q.IsActive)
                .ThenBy(q => q.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeactivateAsync(int id)
        {
            var quest = await _db.Quests.FirstOrDefaultAsync(q => q.Id == id);
            if (quest == null)
                return NotFound();

            quest.IsActive = false;

            await _db.SaveChangesAsync();

            TempData["Message"] = $"Quest '{quest.Title}' was deactivated.";
            return RedirectToPage();
        }

    }
}
