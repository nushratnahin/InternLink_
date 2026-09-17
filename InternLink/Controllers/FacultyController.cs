using InternLink.Data;
using InternLink.Models;
using InternLink.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternLink.Controllers
{
    [Authorize(Roles = Roles.Faculty)]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FacultyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // The "Grading Cockpit": everything waiting on a defense score, plus a history of graded work.
        public async Task<IActionResult> Dashboard()
        {
            var placements = await _context.Placements
                .Include(p => p.Student)
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .Include(p => p.Grade)
                .ToListAsync();

            var readyToGrade = placements
                .Where(p => p.Grade is null && p.Evaluation is { IsSubmitted: true })
                .OrderBy(p => p.Evaluation!.SubmittedDate)
                .ToList();

            var graded = placements
                .Where(p => p.Grade is not null)
                .OrderByDescending(p => p.Grade!.GradedDate)
                .ToList();

            var vm = new FacultyDashboardViewModel
            {
                ReadyToGrade = readyToGrade,
                Graded = graded,
                AverageFinalScore = graded.Any() ? Math.Round(graded.Average(p => p.Grade!.FinalScore), 1) : 0
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GradePlacement(int id)
        {
            var placement = await _context.Placements
                .Include(p => p.Student)
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .Include(p => p.Grade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (placement is null) return NotFound();

            var vm = new GradePlacementViewModel
            {
                PlacementId = placement.Id,
                StudentName = placement.Student?.FullName ?? string.Empty,
                CompanyName = placement.CompanyName,
                LogbookScore = placement.ProgressPercent,
                SupervisorScore = placement.Evaluation?.ScoreOutOf100 ?? 0,
                SupervisorEvaluationSubmitted = placement.Evaluation?.IsSubmitted ?? false,
                DefenseScore = placement.Grade?.DefenseScore ?? 0
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradePlacement(GradePlacementViewModel model)
        {
            var placement = await _context.Placements
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .Include(p => p.Grade)
                .FirstOrDefaultAsync(p => p.Id == model.PlacementId);

            if (placement is null) return NotFound();

            if (placement.Evaluation is null || !placement.Evaluation.IsSubmitted)
            {
                ModelState.AddModelError(string.Empty, "This placement cannot be graded yet - the supervisor evaluation hasn't been submitted.");
            }

            if (!ModelState.IsValid)
            {
                model.StudentName = (await _context.Users.FindAsync(placement.StudentId))?.FullName ?? string.Empty;
                model.CompanyName = placement.CompanyName;
                model.LogbookScore = placement.ProgressPercent;
                model.SupervisorScore = placement.Evaluation?.ScoreOutOf100 ?? 0;
                model.SupervisorEvaluationSubmitted = placement.Evaluation?.IsSubmitted ?? false;
                return View(model);
            }

            var logbookScore = placement.ProgressPercent;
            var supervisorScore = placement.Evaluation!.ScoreOutOf100;
            var finalScore = Math.Round(
                (logbookScore * Grade.LogbookWeight) +
                (supervisorScore * Grade.SupervisorWeight) +
                (model.DefenseScore * Grade.DefenseWeight), 1);

            if (placement.Grade is null)
            {
                placement.Grade = new Grade { PlacementId = placement.Id };
                _context.Grades.Add(placement.Grade);
            }

            var (letterGrade, gradePoint, clearanceStatus) = Grade.Evaluate(finalScore);

            placement.Grade.DefenseScore = model.DefenseScore;
            placement.Grade.LogbookScore = logbookScore;
            placement.Grade.SupervisorScore = supervisorScore;
            placement.Grade.FinalScore = finalScore;
            placement.Grade.LetterGrade = letterGrade;
            placement.Grade.GradePoint = gradePoint;
            placement.Grade.ClearanceStatus = clearanceStatus;
            placement.Grade.GradedByFacultyId = _userManager.GetUserId(User);
            placement.Grade.GradedDate = DateTime.UtcNow;

            placement.Status = finalScore >= Grade.PassingScore ? PlacementStatus.Completed : PlacementStatus.Failed;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Final grade recorded: {finalScore}/100 ({letterGrade}, GPA {gradePoint:0.00}).";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
