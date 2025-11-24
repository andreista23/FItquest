using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using FitQuest.Data;
using FitQuest.Models;
using FitQuest.Pages;
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
        // Schema default de autentificare
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // Schema folosită când dăm Challenge (de ex. la "Login cu Google")
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";          // pagina ta de login clasic
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddGoogle("Google", options =>
    {
        var googleConfig = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleConfig["ClientId"]!;
        options.ClientSecret = googleConfig["ClientSecret"]!;

        // aici putem sincroniza user-ul cu baza de date
        options.Events.OnCreatingTicket = async context =>
        {
            var db = context.HttpContext.RequestServices
                        .GetRequiredService<ApplicationDbContext>();

            var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                var user = await db.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        Name = string.IsNullOrWhiteSpace(name) ? email : name,
                        Role = UserRole.Standard,   // default pentru cont creat prin Google
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }

                // dacă vrei, poți atașa aici și un claim custom cu rolul din DB
                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
