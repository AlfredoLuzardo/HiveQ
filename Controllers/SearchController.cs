using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiveQ.Models;

namespace HiveQ.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Search
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        // GET: Search/Results
        [Authorize]
        public async Task<IActionResult> Results(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                TempData["Error"] = "Please enter a search term.";
                return RedirectToAction("Index");
            }

            // Search for active queues by name or description
            var queues = await _context.Queues
                .Where(q => q.IsActive && q.Status != "Closed" && q.Status != "Paused")
                .Where(q => q.QueueName.Contains(searchQuery) || 
                           (q.Description != null && q.Description.Contains(searchQuery)))
                .Include(q => q.User)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.ResultCount = queues.Count;

            return View(queues);
        }

        // GET: Search/QueueDetails/5
        [Authorize]
        public async Task<IActionResult> QueueDetails(int id)
        {
            var queue = await _context.Queues
                .Include(q => q.User)
                .Include(q => q.QueueEntries.Where(qe => qe.Status == "Waiting" || qe.Status == "Notified"))
                .FirstOrDefaultAsync(q => q.QueueId == id && q.IsActive);

            if (queue == null)
            {
                TempData["Error"] = "Queue not found or is no longer active.";
                return RedirectToAction("Index");
            }

            // Calculate current wait time
            var currentQueueSize = queue.QueueEntries.Count(qe => qe.Status == "Waiting" || qe.Status == "Notified");
            var estimatedWaitMinutes = currentQueueSize * queue.EstimatedWaitTimePerPerson;

            ViewBag.CurrentQueueSize = currentQueueSize;
            ViewBag.EstimatedWaitMinutes = estimatedWaitMinutes;
            ViewBag.QRCodeData = queue.QRCodeData;

            return View(queue);
        }
    }
}
