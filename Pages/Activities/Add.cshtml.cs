using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FitQuest.Pages.Activities
{
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AddModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Modelul folosit de formular pentru activitate
        [BindProperty]
        public ActivityInput Input { get; set; } = new();

        // Fișierul video trimis (opțional)
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
            // doar afișăm formularul
            ViewData["Debug"] = "OnGet() – formular încărcat.";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["Debug"] = "OnPostAsync START";

            // 1. Validare model
            if (!ModelState.IsValid)
            {
                ViewData["Debug"] = "ModelState INVALID.";
                return Page();
            }

            // 2. Verificăm dacă user-ul este autentificat
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
            {
                ViewData["Debug"] = "User NU este autentificat, redirect la Login.";
                return RedirectToPage("/Account/Login");
            }

            // 3. Luăm ID-ul user-ului din claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                ViewData["Debug"] = $"Nu am putut parsa userId din ClaimTypes.NameIdentifier. Valoare: '{userIdString ?? "NULL"}'";
                ModelState.AddModelError(string.Empty, "Nu am putut identifica utilizatorul curent.");
                return Page();
            }

            // 4. Creăm activitatea
            var activity = new Activity
            {
                UserId = userId,
                Type = Input.Type,
                Duration = Input.Duration,
                Date = Input.Date,
                Status = ActivityStatus.Pending // dovada urmează să fie validată
            };

            _db.Activities.Add(activity);
            var rowsActivity = await _db.SaveChangesAsync(); // avem acum activity.Id

            ViewData["Debug"] = $"Activity salvată. rows={rowsActivity}, Activity.Id={activity.Id}, UserId={userId}";

            // 5. Dacă user-ul a trimis video, îl salvăm ca Evidence (opțional)
            if (EvidenceFile != null && EvidenceFile.Length > 0)
            {
                // verificăm să fie video
                if (!EvidenceFile.ContentType.StartsWith("video/"))
                {
                    ModelState.AddModelError("EvidenceFile", "Fișierul trebuie să fie de tip video.");
                    ViewData["Debug"] = "EvidenceFile prezent, dar nu este video.";
                    return Page();
                }

                // 2. limită de mărime: 30 MB
                long maxSizeBytes = 30L * 1024L * 1024L; // 30 MB

                if (EvidenceFile.Length > maxSizeBytes)
                {
                    ModelState.AddModelError("EvidenceFile", "Fișierul nu poate depăși 30 MB.");
                    return Page();
                }

                // folderul unde salvăm fișierele: wwwroot/evidence
                var evidenceFolder = Path.Combine(_env.WebRootPath, "evidence");
                Directory.CreateDirectory(evidenceFolder);

                var extension = Path.GetExtension(EvidenceFile.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var physicalPath = Path.Combine(evidenceFolder, fileName);

                // salvăm fișierul pe disc
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await EvidenceFile.CopyToAsync(stream);
                }

                // cale relativă pentru acces din browser (ex: /evidence/abc123.mp4)
                var relativePath = $"/evidence/{fileName}";

                var evidence = new Evidence
                {
                    ActivityId = activity.Id,
                    FilePath = relativePath,
                    UploadedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Validated = false
                };

                // ATENȚIE: numele DbSet-ului din ApplicationDbContext
                // dacă se numește Evidences:
                _db.Evidence.Add(evidence);

                // dacă la tine e alt nume (ex. Evidence), folosește-l pe acela:
                // _db.Evidence.Add(evidence);

                var rowsEvidence = await _db.SaveChangesAsync();

                ViewData["Debug"] = $"Activity + Evidence salvate. ActivityId={activity.Id}, File={relativePath}, rowsEvidence={rowsEvidence}";
            }
            else
            {
                ViewData["Debug"] = $"Activity salvată FĂRĂ evidence. ActivityId={activity.Id}";
            }

            // rămânem pe pagină ca să vezi mesajul din ViewData["Debug"]
            return Page();
        }
    }
}
