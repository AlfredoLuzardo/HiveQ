using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiveQ.Models;

namespace HiveQ.Controllers
{
    public class ManageQueuesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManageQueuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ManageQueues
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Get logged-in user's email
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "User authentication error.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            // Load user's queues
            var queues = await _context.Queues
                .Where(q => q.UserId == currentUser.UserId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(queues);
        }

        // GET: ManageQueues/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "User authentication error.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var queue = await _context.Queues
                .Include(q => q.QueueEntries.Where(qe => qe.Status == "Waiting" || qe.Status == "Notified"))
                .ThenInclude(qe => qe.User)
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
            {
                TempData["Error"] = "Queue not found.";
                return RedirectToAction("Index");
            }

            // Sort queue entries by position
            ViewBag.QueueEntries = queue.QueueEntries.OrderBy(qe => qe.PositionNumber).ToList();

            return View(queue);
        }

        // POST: ManageQueues/CallNext/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CallNext(int id)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User authentication error." });
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var queue = await _context.Queues
                .Include(q => q.QueueEntries)
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
            {
                return Json(new { success = false, message = "Queue not found." });
            }

            // Find the first person waiting
            var nextEntry = queue.QueueEntries
                .Where(qe => qe.Status == "Waiting")
                .OrderBy(qe => qe.PositionNumber)
                .FirstOrDefault();

            if (nextEntry == null)
            {
                return Json(new { success = false, message = "No one is waiting in the queue." });
            }

            // Update status to Notified
            nextEntry.Status = "Notified";
            nextEntry.NotifiedAt = DateTime.UtcNow;
            queue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Called {nextEntry.User?.FirstName ?? "customer"}!" });
        }

        // POST: ManageQueues/MarkServed/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkServed(int queueEntryId)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "User authentication error." });
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var queueEntry = await _context.QueueEntries
                .Include(qe => qe.Queue)
                .Include(qe => qe.User)
                .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId && qe.Queue.UserId == currentUser.UserId);

            if (queueEntry == null)
            {
                return Json(new { success = false, message = "Queue entry not found." });
            }

            // Mark as served
            queueEntry.Status = "Served";
            queueEntry.ServedAt = DateTime.UtcNow;
            queueEntry.Queue.CurrentQueueSize--;
            queueEntry.Queue.TotalServedToday++;
            queueEntry.Queue.UpdatedAt = DateTime.UtcNow;

            // Auto-delete guest user if they have no other queue entries
            if (queueEntry.User.PasswordHash == "GUEST_USER")
            {
                var hasOtherEntries = await _context.QueueEntries
                    .AnyAsync(qe => qe.UserId == queueEntry.UserId && 
                                   qe.QueueEntryId != queueEntryId && 
                                   (qe.Status == "Waiting" || qe.Status == "Notified"));

                if (!hasOtherEntries)
                {
                    // Guest user has no other active queue entries, safe to delete
                    _context.Users.Remove(queueEntry.User);
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Customer marked as served." });
        }

        // POST: ManageQueues/DeleteQueue/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteQueue(int id)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "User authentication error.";
                return RedirectToAction("Index");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue != null)
            {
                queue.IsActive = false;
                queue.Status = "Closed";
                queue.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["Message"] = "Queue deleted successfully";
            }

            return RedirectToAction("Index");
        }

        // GET: ManageQueues/EditQueue/5
        [Authorize]
        public async Task<IActionResult> EditQueue(int id)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "User authentication error.";
                return RedirectToAction("Index");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
            {
                TempData["Error"] = "Queue not found.";
                return RedirectToAction("Index");
            }

            return View(queue);
        }

        // POST: ManageQueues/EditQueue/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditQueue(int id, string queueName, string? description, int maxCapacity, int estimatedTime, string status)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "User authentication error.";
                return RedirectToAction("Index");
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
            {
                TempData["Error"] = "Queue not found.";
                return RedirectToAction("Index");
            }

            queue.QueueName = queueName;
            queue.Description = description;
            queue.MaxCapacity = maxCapacity;
            queue.EstimatedWaitTimePerPerson = estimatedTime;
            queue.Status = status;
            queue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Queue updated successfully";
            return RedirectToAction("Index");
        }
    }
}
