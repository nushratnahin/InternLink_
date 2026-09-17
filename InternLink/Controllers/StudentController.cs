using InternLink.Data;
using InternLink.Models;
using InternLink.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternLink.Services;

namespace InternLink.Controllers
{
    [Authorize(Roles = Roles.Student)]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CertificatePdfService _certificatePdfService;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, CertificatePdfService certificatePdfService)
        {
            _context = context;
            _userManager = userManager;
            _certificatePdfService = certificatePdfService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User)!;

            var placements = await _context.Placements
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .Include(p => p.Grade)
                .Where(p => p.StudentId == userId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(new StudentDashboardViewModel { Placements = placements });
        }

        [HttpGet]
        public IActionResult CreatePlacement()
        {
            return View(new CreatePlacementViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlacement(CreatePlacementViewModel model)
        {
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "End date must be after the start date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;

            var placement = new Placement
            {
                StudentId = userId,
                CompanyName = model.CompanyName,
                Position = model.Position,
                SupervisorName = model.SupervisorName,
                SupervisorEmail = model.SupervisorEmail,
                SupervisorPhone = model.SupervisorPhone,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                RequiredHours = model.RequiredHours,
                Status = PlacementStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Placement created. A coordinator will review it shortly.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> PlacementDetails(int id)
        {
            var placement = await GetOwnedPlacementAsync(id);
            if (placement is null) return NotFound();

            return View(placement);
        }

        public async Task<IActionResult> Logbook(int id)
        {
            var placement = await GetOwnedPlacementAsync(id);
            if (placement is null) return NotFound();

            return View(placement);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLogbookEntry(int placementId)
        {
            var placement = await GetOwnedPlacementAsync(placementId);
            if (placement is null) return NotFound();

            var nextWeek = placement.LogbookEntries.Any()
                ? placement.LogbookEntries.Max(l => l.WeekNumber) + 1
                : 1;

            var weekStart = placement.LogbookEntries.Any()
                ? placement.LogbookEntries.Max(l => l.WeekEndDate).AddDays(1)
                : placement.StartDate;

            return View(new LogbookEntryFormViewModel
            {
                PlacementId = placementId,
                WeekNumber = Math.Min(nextWeek, 12),
                WeekStartDate = weekStart,
                WeekEndDate = weekStart.AddDays(6)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLogbookEntry(LogbookEntryFormViewModel model)
        {
            var placement = await GetOwnedPlacementAsync(model.PlacementId);
            if (placement is null) return NotFound();

            if (!ValidateLogbookForm(model))
            {
                return View(model);
            }

            _context.LogbookEntries.Add(new LogbookEntry
            {
                PlacementId = model.PlacementId,
                WeekNumber = model.WeekNumber,
                WeekStartDate = model.WeekStartDate,
                WeekEndDate = model.WeekEndDate,
                HoursLogged = model.HoursLogged,
                TasksCompleted = model.TasksCompleted,
                SkillsApplied = model.SkillsApplied,
                ChallengesAndLearnings = model.ChallengesAndLearnings,
                Status = model.IsDraft ? LogbookStatus.Draft : LogbookStatus.Submitted,
                SubmittedDate = DateTime.UtcNow
            });

            if (placement.Status == PlacementStatus.Pending)
            {
                placement.Status = PlacementStatus.Active;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = model.IsDraft
                ? $"Week {model.WeekNumber} log saved as a draft."
                : $"Week {model.WeekNumber} log submitted for coordinator review.";
            return RedirectToAction(nameof(Logbook), new { id = model.PlacementId });
        }

        // Spec 2.2: only an Approved entry is locked against student modification -
        // Draft, Submitted, and Flagged entries can still be edited and (re)submitted.
        [HttpGet]
        public async Task<IActionResult> EditLogbookEntry(int id)
        {
            var entry = await GetOwnedLogEntryAsync(id);
            if (entry is null) return NotFound();

            if (entry.Status == LogbookStatus.Approved)
            {
                TempData["Error"] = "This log has already been approved and can no longer be edited.";
                return RedirectToAction(nameof(Logbook), new { id = entry.PlacementId });
            }

            return View(new LogbookEntryFormViewModel
            {
                Id = entry.Id,
                PlacementId = entry.PlacementId,
                WeekNumber = entry.WeekNumber,
                WeekStartDate = entry.WeekStartDate,
                WeekEndDate = entry.WeekEndDate,
                HoursLogged = entry.HoursLogged,
                TasksCompleted = entry.TasksCompleted,
                SkillsApplied = entry.SkillsApplied,
                ChallengesAndLearnings = entry.ChallengesAndLearnings
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLogbookEntry(LogbookEntryFormViewModel model)
        {
            var entry = await GetOwnedLogEntryAsync(model.Id);
            if (entry is null) return NotFound();

            if (entry.Status == LogbookStatus.Approved)
            {
                TempData["Error"] = "This log has already been approved and can no longer be edited.";
                return RedirectToAction(nameof(Logbook), new { id = entry.PlacementId });
            }

            if (!ValidateLogbookForm(model))
            {
                return View(model);
            }

            entry.WeekNumber = model.WeekNumber;
            entry.WeekStartDate = model.WeekStartDate;
            entry.WeekEndDate = model.WeekEndDate;
            entry.HoursLogged = model.HoursLogged;
            entry.TasksCompleted = model.TasksCompleted;
            entry.SkillsApplied = model.SkillsApplied;
            entry.ChallengesAndLearnings = model.ChallengesAndLearnings;
            // Editing a Flagged entry and resubmitting moves it back into the review queue.
            entry.Status = model.IsDraft ? LogbookStatus.Draft : LogbookStatus.Submitted;
            entry.CoordinatorComment = model.IsDraft ? entry.CoordinatorComment : null;
            entry.SubmittedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = model.IsDraft
                ? $"Week {model.WeekNumber} log updated and saved as a draft."
                : $"Week {model.WeekNumber} log updated and resubmitted for review.";
            return RedirectToAction(nameof(Logbook), new { id = entry.PlacementId });
        }

        // Printable clearance certificate - only available once the placement has a final grade.
        public async Task<IActionResult> Clearance(int id)
        {
            var placement = await GetOwnedPlacementAsync(id, includeGradeDetail: true);
            if (placement is null) return NotFound();

            if (placement.Grade is null || placement.Status != PlacementStatus.Completed)
            {
                TempData["Error"] = "Your clearance certificate is not ready yet - it unlocks once faculty finalize your grade.";
                return RedirectToAction(nameof(PlacementDetails), new { id });
            }

            return View(placement);
        }

        // Generates a real PDF file directly. This does not open the browser print dialog.
        public async Task<IActionResult> ClearancePdf(int id)
        {
            var placement = await GetOwnedPlacementAsync(id, includeGradeDetail: true);
            if (placement is null) return NotFound();

            if (placement.Grade is null || placement.Status != PlacementStatus.Completed)
            {
                TempData["Error"] = "Your clearance certificate is not ready yet - it unlocks once faculty finalize your grade.";
                return RedirectToAction(nameof(PlacementDetails), new { id });
            }

            var pdf = _certificatePdfService.Generate(placement);
            var safeStudent = string.IsNullOrWhiteSpace(placement.Student?.FullName)
                ? "Student"
                : string.Join("_", placement.Student.FullName.Split(Path.GetInvalidFileNameChars()));
            var filename = $"InternLink_Clearance_{safeStudent}_{placement.Id}.pdf";
            return File(pdf, "application/pdf", filename);
        }

        // Drafts can be saved with blank/incomplete fields; only a real Submit needs the full detail.
        private bool ValidateLogbookForm(LogbookEntryFormViewModel model)
        {
            if (model.IsDraft)
            {
                ModelState.Clear();
                return true;
            }

            if (model.WeekEndDate <= model.WeekStartDate)
            {
                ModelState.AddModelError(nameof(model.WeekEndDate), "Week end date must be after the start date.");
            }

            if (string.IsNullOrWhiteSpace(model.TasksCompleted))
            {
                ModelState.AddModelError(nameof(model.TasksCompleted), "Describe the tasks you completed this week.");
            }

            return ModelState.IsValid;
        }

        private async Task<Placement?> GetOwnedPlacementAsync(int id, bool includeGradeDetail = false)
        {
            var userId = _userManager.GetUserId(User)!;

            var query = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .AsQueryable();

            if (includeGradeDetail)
            {
                query = query.Include(p => p.Grade).ThenInclude(g => g!.GradedByFaculty);
            }
            else
            {
                query = query.Include(p => p.Grade);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id && p.StudentId == userId);
        }

        private async Task<LogbookEntry?> GetOwnedLogEntryAsync(int logEntryId)
        {
            var userId = _userManager.GetUserId(User)!;

            return await _context.LogbookEntries
                .Include(l => l.Placement)
                .FirstOrDefaultAsync(l => l.Id == logEntryId && l.Placement!.StudentId == userId);
        }
    }
}
