using InternLink.Data;
using InternLink.Models;
using InternLink.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternLink.Controllers
{
    [Authorize(Roles = Roles.Coordinator)]
    public class CoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Spec 2.3 milestone trigger: auto-dispatch the evaluation link once the intern
        // hits the full hours quota or reaches week 10, whichever comes first.
        private const int MilestoneWeekNumber = 10;

        public CoordinatorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var placements = await _context.Placements
                .Include(p => p.Student)
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var pendingLogs = await _context.LogbookEntries
                .Include(l => l.Placement).ThenInclude(p => p!.Student)
                .Where(l => l.Status == LogbookStatus.Submitted)
                .OrderBy(l => l.SubmittedDate)
                .ToListAsync();

            var vm = new CoordinatorDashboardViewModel
            {
                AllPlacements = placements,
                PendingLogs = pendingLogs,
                ActiveCount = placements.Count(p => p.Status == PlacementStatus.Active),
                CompletedCount = placements.Count(p => p.Status == PlacementStatus.Completed),
                PendingLogCount = pendingLogs.Count
            };

            return View(vm);
        }

        public async Task<IActionResult> PlacementDetails(int id)
        {
            var placement = await _context.Placements
                .Include(p => p.Student)
                .Include(p => p.LogbookEntries)
                .Include(p => p.Evaluation)
                .Include(p => p.Grade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (placement is null) return NotFound();

            return View(placement);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToMe(int id)
        {
            var placement = await _context.Placements.FindAsync(id);
            if (placement is null) return NotFound();

            placement.CoordinatorId = _userManager.GetUserId(User);
            if (placement.Status == PlacementStatus.Pending)
            {
                placement.Status = PlacementStatus.Active;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Placement assigned to you.";
            return RedirectToAction(nameof(PlacementDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewLog(ReviewLogbookEntryViewModel model, int returnPlacementId)
        {
            var entry = await _context.LogbookEntries
                .Include(l => l.Placement).ThenInclude(p => p!.LogbookEntries)
                .Include(l => l.Placement).ThenInclude(p => p!.Evaluation)
                .FirstOrDefaultAsync(l => l.Id == model.LogbookEntryId);

            if (entry is null) return NotFound();

            var isApprove = string.Equals(model.Decision, "Approve", StringComparison.OrdinalIgnoreCase);

            if (!isApprove && string.IsNullOrWhiteSpace(model.Comment))
            {
                TempData["Error"] = "Please add a comment explaining what needs revision before flagging a log.";
                return returnPlacementId > 0
                    ? RedirectToAction(nameof(PlacementDetails), new { id = returnPlacementId })
                    : RedirectToAction(nameof(Dashboard));
            }

            entry.Status = isApprove ? LogbookStatus.Approved : LogbookStatus.Flagged;
            entry.CoordinatorComment = model.Comment;
            entry.ReviewedDate = DateTime.UtcNow;
            if (isApprove)
            {
                entry.ApprovedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (isApprove && entry.Placement is not null)
            {
                await EnsureMilestoneEvaluationLinkAsync(entry.Placement);
            }

            TempData["Success"] = $"Week {entry.WeekNumber} log {(isApprove ? "approved" : "flagged for revision")}.";

            return returnPlacementId > 0
                ? RedirectToAction(nameof(PlacementDetails), new { id = returnPlacementId })
                : RedirectToAction(nameof(Dashboard));
        }

        // Creates (or reuses) the token-based evaluation link for a placement's host supervisor.
        // Manual dispatch path from spec 2.3 - the button coordinators can click any time.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateEvaluationLink(int placementId)
        {
            var placement = await _context.Placements
                .Include(p => p.Evaluation)
                .FirstOrDefaultAsync(p => p.Id == placementId);

            if (placement is null) return NotFound();

            if (placement.Evaluation is null)
            {
                placement.Evaluation = new Evaluation
                {
                    PlacementId = placementId,
                    Token = Evaluation.GenerateToken()
                };
                _context.Evaluations.Add(placement.Evaluation);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Evaluation link ready - copy it below and send it to the host supervisor.";
            return RedirectToAction(nameof(PlacementDetails), new { id = placementId });
        }

        // Spec 2.3 Milestone Trigger: automatically generated when the intern achieves
        // 300 approved hours or completes Week 10 - whichever happens first.
        private async Task EnsureMilestoneEvaluationLinkAsync(Placement placement)
        {
            if (placement.Evaluation is not null)
            {
                return; // Already dispatched (manually or by an earlier milestone hit).
            }

            var approvedHours = placement.LogbookEntries
                .Where(l => l.Status == LogbookStatus.Approved)
                .Sum(l => l.HoursLogged);

            var reachedHoursTarget = approvedHours >= placement.RequiredHours;
            var reachedWeekMilestone = placement.LogbookEntries
                .Any(l => l.Status == LogbookStatus.Approved && l.WeekNumber >= MilestoneWeekNumber);

            if (!reachedHoursTarget && !reachedWeekMilestone)
            {
                return;
            }

            placement.Evaluation = new Evaluation
            {
                PlacementId = placement.Id,
                Token = Evaluation.GenerateToken()
            };
            _context.Evaluations.Add(placement.Evaluation);
            await _context.SaveChangesAsync();
        }
    }
}
