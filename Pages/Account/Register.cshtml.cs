using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class RegisterModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public RegisterModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public RegisterInput Input { get; set; }

    public string? ErrorMessage { get; set; }

    public class RegisterInput
    {
        public string Username { get; set; } 
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == Input.Email);
        if (exists)
        {
            ErrorMessage = "Email already in use.";
            return Page();
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password);

        var user = new User
        {
            Email = Input.Email,
            Name = Input.Username,
            PasswordHash = passwordHash,
            Role = UserRole.Standard,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToPage("/Index");
    }
}
