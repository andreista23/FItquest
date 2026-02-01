using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FitQuest.Pages.Account
{
    public class AdminRegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AdminRegisterModel(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [BindProperty]
        public AdminRegisterInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class AdminRegisterInput
        {
            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = "";

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Password is required.")]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
            public string Password { get; set; } = "";

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = "";

            [Required(ErrorMessage = "Admin code is required.")]
            public string AdminCode { get; set; } = "";
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var exists = await _db.Users.AnyAsync(u => u.Email == Input.Email);
            if (exists)
            {
                ErrorMessage = "Email already in use.";
                return Page();
            }

            var secret = _config["AdminAccessCodes:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                ErrorMessage = "Admin secret key is not configured.";
                return Page();
            }

            var hashedInput = Hash(secret, Input.AdminCode);

            // verificăm cod din DB
            var codeRow = await _db.AdminAccessCodes
                .FirstOrDefaultAsync(c =>
                    c.IsActive &&
                    c.CodeHash == hashedInput &&
                    (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow)
                );

            if (codeRow == null)
            {
                ErrorMessage = "Cod admin invalid.";
                return Page();
            }

            // Creează user Admin
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password);

            var user = new User
            {
                Email = Input.Email,
                Name = Input.Username,
                PasswordHash = passwordHash,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);

            // OPTIONAL: fă codul one-time (recomandat)
            // Dacă vrei să fie reutilizabil, comentează linia asta:
            codeRow.IsActive = false;

            await _db.SaveChangesAsync();

            // Auto-login (ca la register-ul normal), dar cu AdminGatePassed = false (încă nu a trecut gate)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            // Îl trimitem imediat în AdminGate
            return RedirectToPage("/Account/AdminGate");
        }

        private static string Hash(string secretKey, string code)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(code.Trim())));
        }
    }
}