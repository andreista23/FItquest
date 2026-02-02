using FitQuest.Data;
using FitQuest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitQuest.Pages.Trainer
{
    [Authorize(Roles = "Trainer")]
    public class MyActivitiesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public MyActivitiesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TrainerActivity> Activities { get; set; } = new();

        // ✅ abonatii trainerului (pentru dropdown)
        public List<SelectListItem> Subscribers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null)
            {
                Activities = new();
                Subscribers = new();
                return;
            }

            Activities = await _db.TrainerActivities
                .Where(a => a.TrainerProfileId == trainerProfile.Id)
                .Include(a => a.Assignments)
                .ToListAsync();

            // ✅ abonatii activi ai trainerului
            var subscriberUsers = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                .Select(s => s.User)
                .Distinct()
                .ToListAsync();

            Subscribers = subscriberUsers
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.Name} (#{u.Id})"
                })
                .ToList();
        }

        // ✅ Assign o activitate unui abonat
        public async Task<IActionResult> OnPostAssignAsync(int activityId, int userId)
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null) return Forbid();

            // securitate: activitatea trebuie sa fie a trainerului curent
            bool activityIsMine = await _db.TrainerActivities
                .AnyAsync(a => a.Id == activityId && a.TrainerProfileId == trainerProfile.Id);

            if (!activityIsMine) return Forbid();

            // securitate: userId trebuie sa fie abonat la trainer
            bool isSubscriber = await _db.Subscriptions
                .AnyAsync(s => s.TrainerId == trainerProfile.Id && s.UserId == userId && s.Status == "active");

            if (!isSubscriber) return BadRequest("User is not your active subscriber.");

            // evitam duplicate
            bool exists = await _db.TrainerActivityAssignments
                .AnyAsync(x => x.UserId == userId && x.TrainerActivityId == activityId);

            if (!exists)
            {
                _db.TrainerActivityAssignments.Add(new TrainerActivityAssignment
                {
                    UserId = userId,
                    TrainerActivityId = activityId
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        // ✅ Assign o activitate tuturor abonatilor
        public async Task<IActionResult> OnPostAssignAllAsync(int activityId)
        {
            var trainerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trainerProfile = await _db.TrainerProfiles
                .FirstOrDefaultAsync(t => t.UserId == trainerUserId);

            if (trainerProfile == null) return Forbid();

            bool activityIsMine = await _db.TrainerActivities
                .AnyAsync(a => a.Id == activityId && a.TrainerProfileId == trainerProfile.Id);

            if (!activityIsMine) return Forbid();

            var subscriberIds = await _db.Subscriptions
                .Where(s => s.TrainerId == trainerProfile.Id && s.Status == "active")
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            // ce exista deja
            var alreadyAssignedIds = await _db.TrainerActivityAssignments
                .Where(x => x.TrainerActivityId == activityId && subscriberIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .ToListAsync();

            var toAdd = subscriberIds
                .Where(uid => !alreadyAssignedIds.Contains(uid))
                .Select(uid => new TrainerActivityAssignment
                {
                    UserId = uid,
                    TrainerActivityId = activityId
                })
                .ToList();

            if (toAdd.Count > 0)
            {
                _db.TrainerActivityAssignments.AddRange(toAdd);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}