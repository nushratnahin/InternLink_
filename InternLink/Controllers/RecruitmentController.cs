using InternLink.Data;
using InternLink.Models;
using InternLink.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternLink.Controllers
{
    // Spec 2.1 "Automated Trigger": a placement is auto-created once an applicant's
    // funnel status reaches Offered or Accepted "within the main recruitment pipeline".
    // That full pipeline (job circulars, resume/ATS screening, interview scheduling) is a
    // separate subsystem outside this spec's scope - this controller is a minimal stand-in
    // so the trigger itself is real and demonstrable, not just described in comments.
    [Authorize]
    public class RecruitmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecruitmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> MyApplications()
        {
            var userId = _userManager.GetUserId(User)!;
            var applications = await _context.RecruitmentApplications
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(applications);
        }

        [Authorize(Roles = Roles.Student)]
        [HttpGet]
        public IActionResult Apply() => View(new CreateApplicationViewModel());

        [Authorize(Roles = Roles.Student)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(CreateApplicationViewModel model)
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

            _context.RecruitmentApplications.Add(new RecruitmentApplication
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
                Status = FunnelStatus.Applied
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Application submitted. A coordinator will move it through the pipeline.";
            return RedirectToAction(nameof(MyApplications));
        }

        // The coordinator's view of every applicant currently in the pipeline.
        [Authorize(Roles = Roles.Coordinator)]
        public async Task<IActionResult> Pipeline()
        {
            var applications = await _context.RecruitmentApplications
                .Include(a => a.Student)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(applications);
        }

        // Spec 2.1: moving an application to Offered or Accepted automatically instantiates
        // the post-placement contract - no separate "create placement" step needed.
        [Authorize(Roles = Roles.Coordinator)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvanceStatus(int id, FunnelStatus status)
        {
            var application = await _context.RecruitmentApplications
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application is null) return NotFound();

            application.Status = status;
            application.StatusUpdatedDate = DateTime.UtcNow;

            if ((status == FunnelStatus.Offered || status == FunnelStatus.Accepted) && application.PlacementId is null)
            {
                var placement = new Placement
                {
                    StudentId = application.StudentId,
                    CompanyName = application.CompanyName,
                    Position = application.Position,
                    SupervisorName = application.SupervisorName,
                    SupervisorEmail = application.SupervisorEmail,
                    SupervisorPhone = application.SupervisorPhone,
                    StartDate = application.StartDate,
                    EndDate = application.EndDate,
                    RequiredHours = application.RequiredHours,
                    Status = PlacementStatus.Active,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Placements.Add(placement);
                await _context.SaveChangesAsync(); // Need placement.Id before linking it below.

                application.PlacementId = placement.Id;
                TempData["Success"] = $"{application.Student?.FullName} reached \"{status}\" — a placement was auto-created and is ready for logbook entries.";
            }
            else
            {
                TempData["Success"] = $"Status updated to {status}.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Pipeline));
        }
    }
}
