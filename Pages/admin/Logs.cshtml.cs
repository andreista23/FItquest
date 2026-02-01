using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class LogsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public LogsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<AdminLog> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            Logs = await _db.AdminLogs
                .Include(l => l.Admin)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
