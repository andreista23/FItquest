using Microsoft.AspNetCore.Authentication.Cookies;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var serverVersion = ServerVersion.AutoDetect(connectionString);
    options.UseMySql(connectionString, serverVersion);
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddGoogle("Google", options =>
    {
        // ia din configuration (user-secrets + appsettings)
        var section = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = section["ClientId"]!;
        options.ClientSecret = section["ClientSecret"]!;

        options.Events.OnCreatingTicket = async context =>
        {
            // sincronziare user din Google în baza ta de date
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.Principal?.FindFirst(ClaimTypes.Name)?.Value;
            var googleId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        Name = string.IsNullOrWhiteSpace(name) ? email : name,
                        GoogleId = googleId,
                        Role = UserRole.Standard,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }
                else
                {
                    // dacă vrei, actualizezi info
                    user.GoogleId ??= googleId;
                    await db.SaveChangesAsync();
                }

                // adaugă claim de rol ca să meargă [Authorize(Roles="Admin")]
                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
            }
        };
    });


var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();