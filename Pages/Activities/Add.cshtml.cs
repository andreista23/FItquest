using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FitQuest.Services;


namespace FitQuest.Pages.Activities
{
    [Authorize]
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly LevelUpService _levelUpService;


        public AddModel(ApplicationDbContext db, IWebHostEnvironment env, LevelUpService levelUpService)
        {
            _db = db;
            _env = env;
            _levelUpService = levelUpService;
        }


        [BindProperty]
        public ActivityInput Input { get; set; } = new();

        [BindProperty]
        public IFormFile? EvidenceFile { get; set; }

        public class ActivityInput
        {
            [Required]
            [Display(Name = "Tip activitate")]
            [MaxLength(100)]
            public string Type { get; set; } = string.Empty;

            [Required]
            [Range(1, 600, ErrorMessage = "Durata trebuie să fie între 1 și 600 de minute.")]
            [Display(Name = "Durată (minute)")]
            public int Duration { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Data activității")]
            public DateTime Date { get; set; } = DateTime.Today;
        }

        public void OnGet()
        {
            ViewData["Debug"] = "OnGet() – formular încărcat.";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["Debug"] = "OnPostAsync START";

            if (!ModelState.IsValid)
            {
                ViewData["Debug"] = "ModelState INVALID.";
                return Page();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                ModelState.AddModelError(string.Empty, "Nu am putut identifica utilizatorul curent.");
                ViewData["Debug"] = $"Nu am putut parsa userId. Valoare: '{userIdString ?? "NULL"}'";
                return Page();
            }

            // ✅ luăm user-ul din DB (pentru XP)
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                ViewData["Debug"] = $"User cu Id={userId} nu există în DB.";
                return Page();
            }

            bool hasVideo = EvidenceFile != null && EvidenceFile.Length > 0;

            // ✅ calculăm XP full o singură dată și îl salvăm în Activity
            int fullXp = CalculateFullXp(Input.Duration);

            var activity = new Activity
            {
                UserId = userId,
                Type = Input.Type,
                Duration = Input.Duration,
                Date = Input.Date,
                FullXp = fullXp,
                XpAwarded = false,
                Status = hasVideo ? ActivityStatus.Pending : ActivityStatus.Approved
            };

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync(); // acum avem activity.Id


            // ✅ dacă are video -> salvăm Evidence (XP se dă la validare)
            if (hasVideo)
            {
                if (!EvidenceFile!.ContentType.StartsWith("video/"))
                {
                    ModelState.AddModelError("EvidenceFile", "Fișierul trebuie să fie de tip video.");
                    ViewData["Debug"] = "EvidenceFile prezent, dar nu este video.";
                    return Page();
                }

                long maxSizeBytes = 30L * 1024L * 1024L; // 30 MB
                if (EvidenceFile.Length > maxSizeBytes)
                {
                    ModelState.AddModelError("EvidenceFile", "Fișierul nu poate depăși 30 MB.");
                    ViewData["Debug"] = $"EvidenceFile prea mare: {EvidenceFile.Length} bytes.";
                    return Page();
                }

                var evidenceFolder = Path.Combine(_env.WebRootPath, "evidence");
                Directory.CreateDirectory(evidenceFolder);

                var extension = Path.GetExtension(EvidenceFile.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var physicalPath = Path.Combine(evidenceFolder, fileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await EvidenceFile.CopyToAsync(stream);
                }

                var relativePath = $"/evidence/{fileName}";

                var evidence = new Evidence
                {
                    ActivityId = activity.Id,
                    FilePath = relativePath,
                    UploadedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Validated = false
                };

                _db.Evidence.Add(evidence);
                await _db.SaveChangesAsync();

                ViewData["Debug"] = $"Activity + Evidence salvate. ActivityId={activity.Id}, FullXp={fullXp}, File={relativePath}";
                return Page();
            }

            // ✅ fără video -> half XP instant + XPEvent
            int halfXp = fullXp / 2;

            bool alreadyGiven = await _db.XPEvents.AnyAsync(x => x.ActivityId == activity.Id);
            if (!alreadyGiven)
            {
                user.Xp += halfXp;

                _db.XPEvents.Add(new XPEvent
                {
                    UserId = user.Id,
                    ActivityId = activity.Id,
                    XPValue = halfXp,
                    Reason = $"Half XP for activity without video. FullXP={fullXp}"
                });

                activity.XpAwarded = true;
                await _db.SaveChangesAsync();
                await _levelUpService.CheckAndNotifyAsync(userId);
            }

            ViewData["Debug"] = $"Activity Approved (no evidence). ActivityId={activity.Id}, FullXp={fullXp}, Awarded={halfXp}";
            return Page();
        }

        private static int CalculateFullXp(int durationMinutes)
        {
            int baseXp = Random.Shared.Next(40, 81); // 40–80
            int durationBonus = durationMinutes * 2; // 2 XP / minut
            int fullXp = baseXp + durationBonus;
            return Math.Clamp(fullXp, 30, 300);
        }
    }
}
