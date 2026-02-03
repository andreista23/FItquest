using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FitQuest.Pages.Admin.Quests
{
    [Authorize(Roles = "Admin")]
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public AddModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, MaxLength(100)]
            public string Title { get; set; } = string.Empty;

            [MaxLength(500)]
            public string? Description { get; set; }

            [Range(0, 100000)]
            public int RewardXP { get; set; }

            [Required]
            public QuestType Type { get; set; }

            [Range(1, 100000)]
            public int Target { get; set; }

            // ✅ enum, nu string
            public QuestPeriod Period { get; set; } = QuestPeriod.Lifetime;

            public bool IsActive { get; set; } = true;
        }


        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var quest = new Quest
            {
                Title = Input.Title,
                Description = Input.Description,
                RewardXP = Input.RewardXP,
                Period = Input.Period,
                Type = Input.Type,
                Target = Input.Target,
                IsActive = Input.IsActive
            };

            _db.Quests.Add(quest);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Quest added.";
            return RedirectToPage("Index");
        }
    }
}
