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

builder.Services.AddScoped<FitQuest.Services.LevelUpService>();
builder.Services.AddHostedService<FitQuest.Services.EvidenceCleanupService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminWithGate", policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("AdminGatePassed", "true");
    });
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
       
        var section = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = section["ClientId"]!;
        options.ClientSecret = section["ClientSecret"]!;

        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

        options.SaveTokens = true;

        options.Events.OnCreatingTicket = async context =>
        {
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
                    user.GoogleId ??= googleId;
                    await db.SaveChangesAsync();
                }

                var identity = (ClaimsIdentity)context.Principal!.Identity!;

                var existingNameId = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (existingNameId != null)
                {
                    identity.RemoveClaim(existingNameId);
                }

                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

                if (!string.IsNullOrEmpty(googleId))
                {
                    identity.AddClaim(new Claim("GoogleId", googleId));
                }

                if (identity.FindFirst(ClaimTypes.Email) == null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                }

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