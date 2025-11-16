using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiveQ.Models;
using System.Diagnostics;

namespace HiveQ.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Home - Shows list of queues
        public async Task<IActionResult> Index()
        {
            var queues = await _context.Queues
                .Where(q => q.IsActive && q.Status != "Closed")
                .Include(q => q.User)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(queues);
        }

        // GET: Home/ViewQueuePos/5
        public IActionResult ViewQueuePos(int id)
        {
            // TODO: Load queue position from database
            return View();
        }

        // GET: Home/ViewQueue/5
        public IActionResult ViewQueue(int id)
        {
            // TODO: Load queue details from database
            return View();
        }

        // POST: Home/LeaveQueue/5
        [HttpPost]
        public IActionResult LeaveQueue(int id)
        {
            // TODO: Implement leave queue logic
            TempData["Message"] = "You have left the queue";
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
