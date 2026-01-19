using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitQuest.Pages.Trainer
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public RegisterModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";

            public IFormFile CvFile { get; set; } = null!;
            public IFormFile RecommendationFile { get; set; } = null!;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (await _db.Users.AnyAsync(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("", "Email already exists.");
                return Page();
            }

            // 1️⃣ CREARE USER
            var user = new User
            {
                Name = Input.Name,
                Email = Input.Email,
                Role = UserRole.Trainer,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 2️⃣ UPLOAD DOCUMENTE
            var folder = Path.Combine(_env.WebRootPath, "trainer_docs");
            Directory.CreateDirectory(folder);

            string cvPath = Path.Combine("trainer_docs", $"{user.Id}_cv.pdf");
            using (var stream = new FileStream(Path.Combine(_env.WebRootPath, cvPath), FileMode.Create))
                await Input.CvFile.CopyToAsync(stream);

            string recPath = Path.Combine("trainer_docs", $"{user.Id}_rec.pdf");
            using (var stream = new FileStream(Path.Combine(_env.WebRootPath, recPath), FileMode.Create))
                await Input.RecommendationFile.CopyToAsync(stream);

            // 3️⃣ CREARE TRAINER PROFILE
            _db.TrainerProfiles.Add(new TrainerProfile
            {
                UserId = user.Id,
                CvPath = cvPath,
                RecommendationPath = recPath,
                IsApproved = false
            });

            // 🔔 NOTIFICĂM ADMINII – TRAINER NOU
            var admins = await _db.Users
                .Where(u => u.Role == UserRole.Admin)
                .ToListAsync();

            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Message = $"🆕 New trainer registration: {user.Email}"
                });
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }
    }
}
