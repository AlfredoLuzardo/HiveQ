using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiveQ.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace HiveQ.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
            
        /// <summary>
        /// GET: Home/Index
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var queues = await _context.Queues
                .Where(q => q.IsActive && q.Status != "Closed")
                .Include(q => q.User)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            ViewBag.Name = HttpContext.User.Identity?.Name;
            return View(queues);
        }

        /// <summary>
        /// GET: Home/ViewQueuePos
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public IActionResult ViewQueuePos(int id)
        {
            // TODO: Load queue position from database
            return View();
        }

        /// <summary>
        /// GET: Home/ViewQueue
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public IActionResult ViewQueue(int id)
        {
            // TODO: Load queue details from database
            return View();
        }

        /// <summary>
        /// POST: Home/LeaveQueue        
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult LeaveQueue(int id)
        {
            // TODO: Implement leave queue logic
            TempData["Message"] = "You have left the queue";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// GET: Home/Error
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
