using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BCrypt.Net;

public class RegisterModel : PageModel
{
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

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        // TODO: verifică dacă email există în DB
        // var exists = _db.Users.Any(u => u.Email == Input.Email);

        bool exists = false; // scoți după ce ai DB

        if (exists)
        {
            ErrorMessage = "Email already in use.";
            return Page();
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password);

        // TODO: salvează user în DB
        // var user = new User { Email = Input.Email, Username = Input.Username, PasswordHash = passwordHash };
        // _db.Users.Add(user);
        // _db.SaveChanges();

        return RedirectToPage("/Account/Login");
    }
}
