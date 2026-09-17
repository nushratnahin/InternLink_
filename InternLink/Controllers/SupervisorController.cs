using InternLink.Data;
using InternLink.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternLink.Controllers
{
    // No [Authorize] here on purpose: host-company supervisors are external users
    // who never create an account. The GUID token in the URL *is* their credential,
    // matching the "no login required" promise from the landing page.
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupervisorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Evaluate(string token)
        {
            var evaluation = await _context.Evaluations
                .Include(e => e.Placement).ThenInclude(p => p!.Student)
                .FirstOrDefaultAsync(e => e.Token == token);

            if (evaluation is null)
            {
                return View("InvalidLink");
            }

            if (evaluation.IsSubmitted)
            {
                return View("AlreadySubmitted", evaluation);
            }

            // Spec 2.3: token carries a 30-day expiration window.
            if (DateTime.UtcNow > evaluation.ExpiresAt)
            {
                return View("LinkExpired");
            }

            var vm = new SubmitEvaluationViewModel
            {
                Token = token,
                StudentName = evaluation.Placement?.Student?.FullName ?? "the student",
                CompanyName = evaluation.Placement?.CompanyName ?? string.Empty,
                Position = evaluation.Placement?.Position ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluate(SubmitEvaluationViewModel model)
        {
            var evaluation = await _context.Evaluations
                .Include(e => e.Placement).ThenInclude(p => p!.Student)
                .FirstOrDefaultAsync(e => e.Token == model.Token);

            if (evaluation is null)
            {
                return View("InvalidLink");
            }

            if (evaluation.IsSubmitted)
            {
                return View("AlreadySubmitted", evaluation);
            }

            if (DateTime.UtcNow > evaluation.ExpiresAt)
            {
                return View("LinkExpired");
            }

            if (!ModelState.IsValid)
            {
                model.StudentName = evaluation.Placement?.Student?.FullName ?? "the student";
                model.CompanyName = evaluation.Placement?.CompanyName ?? string.Empty;
                model.Position = evaluation.Placement?.Position ?? string.Empty;
                return View(model);
            }

            evaluation.TechnicalCompetencyScore = model.TechnicalCompetencyScore;
            evaluation.ProblemSolvingAutonomyScore = model.ProblemSolvingAutonomyScore;
            evaluation.TeamCollaborationScore = model.TeamCollaborationScore;
            evaluation.PunctualityReliabilityScore = model.PunctualityReliabilityScore;
            evaluation.AdaptabilityLearningScore = model.AdaptabilityLearningScore;
            evaluation.ProfessionalConductScore = model.ProfessionalConductScore;
            evaluation.KeyStrengths = model.KeyStrengths;
            evaluation.AreasForImprovement = model.AreasForImprovement;
            evaluation.Recommendation = model.Recommendation;

            // Spec 2.3: single-submission guarantee - token is permanently invalidated on submit.
            evaluation.IsSubmitted = true;
            evaluation.SubmittedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return View("ThankYou", evaluation);
        }
    }
}
