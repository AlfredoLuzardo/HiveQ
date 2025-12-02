using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HiveQ.Models;
using HiveQ.Services;
using HiveQ.Hubs;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace HiveQ.Controllers
{
    public class ManageQueuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authService;
        private readonly IHubContext<QueueHub> _hubContext;

        public ManageQueuesController(ApplicationDbContext context, AuthenticationService authService, IHubContext<QueueHub> hubContext)
        {
            _context = context;
            _authService = authService;
            _hubContext = hubContext;
        }

        private async Task<User?> GetAuthenticatedUserAsync()
        {
            var user = await _authService.GetCurrentUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User authentication error.";
            }
            return user;
        }

        // GET: ManageQueues
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var queues = await _context.Queues
                .Where(q => q.UserId == currentUser.UserId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            // Generate QR codes for each queue
            var queueQRCodes = new Dictionary<int, string>();
            foreach (var queue in queues)
            {
                string joinUrl = $"{Request.Scheme}://{Request.Host}/JoinQueue?code={queue.QRCodeData}";
                
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeDataObj = qrGenerator.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrCodeDataObj))
                    {
                        using (Bitmap qrCodeImage = qrCode.GetGraphic(10))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                qrCodeImage.Save(ms, ImageFormat.Png);
                                byte[] qrCodeBytes = ms.ToArray();
                                string qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                                queueQRCodes[queue.QueueId] = $"data:image/png;base64,{qrCodeBase64}";
                            }
                        }
                    }
                }
            }

            ViewBag.QueueQRCodes = queueQRCodes;
            return View(queues);
        }

        // GET: ManageQueues/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var queue = await _context.Queues
                .Include(q => q.QueueEntries.Where(qe => qe.Status == "Waiting" || qe.Status == "Notified"))
                .ThenInclude(qe => qe.User)
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
            {
                TempData["Error"] = "Queue not found.";
                return RedirectToAction("Index");
            }

            ViewBag.QueueEntries = queue.QueueEntries.OrderBy(qe => qe.PositionNumber).ToList();
            return View(queue);
        }

        // POST: ManageQueues/CallNext/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CallNext(int id)
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return Json(new { success = false, message = "User authentication error." });

            var queue = await _context.Queues
                .Include(q => q.QueueEntries)
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
                return Json(new { success = false, message = "Queue not found." });

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

            // Notify all clients about queue update via SignalR
            await _hubContext.Clients.Group($"Queue_{queue.QueueId}").SendAsync("QueueUpdated", queue.QueueId);
            await _hubContext.Clients.Group("AllQueues").SendAsync("QueueUpdated", queue.QueueId);

            return Json(new { success = true, message = $"Called {nextEntry.User?.FirstName ?? "customer"}!" });
        }

        // POST: ManageQueues/MarkServed/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkServed(int queueEntryId)
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return Json(new { success = false, message = "User authentication error." });

            var queueEntry = await _context.QueueEntries
                .Include(qe => qe.Queue)
                .Include(qe => qe.User)
                .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId && qe.Queue.UserId == currentUser.UserId);

            if (queueEntry == null)
                return Json(new { success = false, message = "Queue entry not found." });

            // Mark as served
            queueEntry.Status = "Served";
            queueEntry.ServedAt = DateTime.UtcNow;
            queueEntry.Queue.CurrentQueueSize--;
            queueEntry.Queue.TotalServedToday++;
            queueEntry.Queue.UpdatedAt = DateTime.UtcNow;
            var queueId = queueEntry.Queue.QueueId;

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

            // Notify all clients about queue update via SignalR
            await _hubContext.Clients.Group($"Queue_{queueId}").SendAsync("QueueUpdated", queueId);
            await _hubContext.Clients.Group("AllQueues").SendAsync("QueueUpdated", queueId);

            return Json(new { success = true, message = "Customer marked as served." });
        }

        // POST: ManageQueues/DeleteQueue/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteQueue(int id)
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Index");

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
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Index");

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
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Index");

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

            // Notify all clients about queue update via SignalR
            await _hubContext.Clients.Group($"Queue_{queue.QueueId}").SendAsync("QueueUpdated", queue.QueueId);
            await _hubContext.Clients.Group("AllQueues").SendAsync("QueueUpdated", queue.QueueId);

            TempData["Message"] = "Queue updated successfully";
            return RedirectToAction("Index");
        }
    }
}
