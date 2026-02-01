using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FitQuest.Pages.Account
{
    public class AdminGateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AdminGateModel(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [BindProperty]
        public string Code { get; set; } = "";

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            // doar adminii ajung aici
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Index");

            // dacă a trecut deja gate, îl ducem în admin
            if (User.HasClaim("AdminGatePassed", "true"))
                return RedirectToPage("/Admin/Dashboard");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Index");

            var secret = _config["AdminAccessCodes:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                ErrorMessage = "Admin secret key is not configured.";
                return Page();
            }

            var hashedInput = Hash(secret, Code);

            var valid = await _db.AdminAccessCodes.AnyAsync(c =>
                c.IsActive &&
                c.CodeHash == hashedInput &&
                (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow)
            );

            if (!valid)
            {
                ErrorMessage = "Cod invalid.";
                return Page();
            }

            // setăm claim-ul în cookie (re-sign-in)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                ErrorMessage = "Invalid session.";
                return Page();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("AdminGatePassed", "true"),
        new Claim("AdminGateAt", DateTime.UtcNow.ToString("O"))
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            _db.AdminLogs.Add(new AdminLog
            {
                AdminId = user.Id,
                Action = "Admin gate passed",
                Target = user.Email
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Admin/Dashboard");
        }

        private static string Hash(string secretKey, string code)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(code.Trim()));
            return Convert.ToBase64String(bytes);
        }
    }
}