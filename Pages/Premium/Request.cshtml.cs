using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Premium
{
    [Authorize]
    public class RequestModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public RequestModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public string UserEmail { get; set; } = "";

        [BindProperty]
        public IFormFile PaymentProof { get; set; } = null!;

        public void OnGet()
        {
            UserEmail = User.Identity!.Name!;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var folder = Path.Combine(_env.WebRootPath, "premium_proofs");
            Directory.CreateDirectory(folder);

            var path = Path.Combine("premium_proofs", $"{userId}_payment.pdf");
            using (var fs = new FileStream(Path.Combine(_env.WebRootPath, path), FileMode.Create))
            {
                await PaymentProof.CopyToAsync(fs);
            }

            _db.PremiumRequests.Add(new PremiumRequest
            {
                UserId = userId,
                PaymentProofPath = path,
                Status = "Pending"
            });

            // 🔔 notificăm adminii
            var admins = await _db.Users
                .Where(u => u.Role == UserRole.Admin)
                .ToListAsync();

            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Message = $"💳 New Premium request from user #{userId}"
                });
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
