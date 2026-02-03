using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FitQuest.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public LoginModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class LoginInput
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet(string? error = null)
        {
            if (!string.IsNullOrEmpty(error))
                ErrorMessage = error;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == Input.Email);

            if (user == null)
            {
                ErrorMessage = "Contul nu există.";
                return Page();
            }

            if (user.IsBanned)
            {
                ErrorMessage = "Your account has been banned by an administrator.";
                return Page();
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ErrorMessage = "Contul nu are parolă setată.";
                return Page();
            }

            bool validPassword = BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash);

            if (!validPassword)
            {
                ErrorMessage = "Parolă incorectă!";
                return Page();
            }

            // CLAIMS
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            var questService = HttpContext.RequestServices
                .GetRequiredService<FitQuest.Services.QuestService>();

            await questService.OnUserLoggedInAsync(user.Id);

            // NOTIFICĂM ADMINII CĂ S-A LOGAT CINEVA
            var admins = await _db.Users
                .Where(u => u.Role == UserRole.Admin)
                .ToListAsync();

            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Message = $"🔐 Login: {user.Email} ({user.Role})"
                });
            }

            //  AUDIT LOGIN ADMIN
            if (user.Role == UserRole.Admin)
            {
                _db.AdminLogs.Add(new AdminLog
                {
                    AdminId = user.Id,
                    Action = "Admin login",
                    Target = user.Email
                });
            }

            await _db.SaveChangesAsync();

            //  REDIRECT SPECIAL ADMIN
            if (user.Role == UserRole.Admin)
                return RedirectToPage("/Account/AdminGate");

            return RedirectToPage("/Index");
        }
    }
}
