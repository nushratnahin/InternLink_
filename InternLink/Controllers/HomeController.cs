using InternLink.Data;
using InternLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternLink.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Public landing page - features + a live snapshot of platform-wide stats,
        // mirroring the original static dashboard numbers but now real.
        public async Task<IActionResult> Index()
        {
            ViewBag.ActivePlacements = await _context.Placements.CountAsync(p => p.Status == PlacementStatus.Active);
            ViewBag.CompletedPlacements = await _context.Placements.CountAsync(p => p.Status == PlacementStatus.Completed);
            ViewBag.PendingReviews = await _context.LogbookEntries.CountAsync(l => l.Status == LogbookStatus.Submitted);

            return View();
        }

        public IActionResult Roles()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
