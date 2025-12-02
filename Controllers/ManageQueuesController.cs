using System.Drawing;
using System.Drawing.Imaging;
using HiveQ.Models;
using HiveQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace HiveQ.Controllers
{
    public class ManageQueuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authService;
        private readonly ISmsService _smsService;
        private readonly ILogger<ManageQueuesController> _logger;

        public ManageQueuesController(
            ApplicationDbContext context,
            AuthenticationService authService,
            ISmsService smsService,
            ILogger<ManageQueuesController> logger
        )
        {
            _context = context;
            _authService = authService;
            _smsService = smsService;
            _logger = logger;
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

            var queues = await _context
                .Queues.Where(q => q.UserId == currentUser.UserId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            // Generate QR codes for each queue
            var queueQRCodes = new Dictionary<int, string>();
            foreach (var queue in queues)
            {
                string joinUrl =
                    $"{Request.Scheme}://{Request.Host}/JoinQueue?code={queue.QRCodeData}";

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeDataObj = qrGenerator.CreateQrCode(
                        joinUrl,
                        QRCodeGenerator.ECCLevel.Q
                    );
                    using (QRCode qrCode = new QRCode(qrCodeDataObj))
                    {
                        using (Bitmap qrCodeImage = qrCode.GetGraphic(10))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                qrCodeImage.Save(ms, ImageFormat.Png);
                                byte[] qrCodeBytes = ms.ToArray();
                                string qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                                queueQRCodes[queue.QueueId] =
                                    $"data:image/png;base64,{qrCodeBase64}";
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

            var queue = await _context
                .Queues.Include(q =>
                    q.QueueEntries.Where(qe => qe.Status == "Waiting" || qe.Status == "Notified")
                )
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

            var queue = await _context
                .Queues.Include(q => q.QueueEntries)
                .FirstOrDefaultAsync(q => q.QueueId == id && q.UserId == currentUser.UserId);

            if (queue == null)
                return Json(new { success = false, message = "Queue not found." });

            // Find the first person waiting
            var nextEntry = queue
                .QueueEntries.Where(qe => qe.Status == "Waiting")
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

            await NotifyCalled(queue.QueueId);

            return Json(
                new
                {
                    success = true,
                    message = $"Called {nextEntry.User?.FirstName ?? "customer"}!",
                }
            );
        }

        // POST: ManageQueues/MarkServed/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkServed(int queueEntryId)
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return Json(new { success = false, message = "User authentication error." });

            var queueEntry = await _context
                .QueueEntries.Include(qe => qe.Queue)
                .Include(qe => qe.User)
                .FirstOrDefaultAsync(qe =>
                    qe.QueueEntryId == queueEntryId && qe.Queue.UserId == currentUser.UserId
                );

            if (queueEntry == null)
                return Json(new { success = false, message = "Queue entry not found." });

            await NotifyTopUsersAsync(queueEntry.QueueId);

            // Mark as served
            queueEntry.Status = "Served";
            queueEntry.ServedAt = DateTime.UtcNow;
            queueEntry.Queue.CurrentQueueSize--;
            queueEntry.Queue.TotalServedToday++;
            queueEntry.Queue.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Auto-delete guest user if they have no other queue entries
            if (queueEntry.User.PasswordHash == "GUEST_USER")
            {
                var hasOtherEntries = await _context.QueueEntries.AnyAsync(qe =>
                    qe.UserId == queueEntry.UserId
                    && qe.QueueEntryId != queueEntryId
                    && (qe.Status == "Waiting" || qe.Status == "Notified")
                );

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
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Index");

            var queue = await _context.Queues.FirstOrDefaultAsync(q =>
                q.QueueId == id && q.UserId == currentUser.UserId
            );

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

            var queue = await _context.Queues.FirstOrDefaultAsync(q =>
                q.QueueId == id && q.UserId == currentUser.UserId
            );

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
        public async Task<IActionResult> EditQueue(
            int id,
            string queueName,
            string? description,
            int maxCapacity,
            int estimatedTime,
            string status
        )
        {
            var currentUser = await GetAuthenticatedUserAsync();
            if (currentUser == null)
                return RedirectToAction("Index");

            var queue = await _context.Queues.FirstOrDefaultAsync(q =>
                q.QueueId == id && q.UserId == currentUser.UserId
            );

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

        /// <summary>
        /// Send notification to the user who has been called
        /// </summary>
        /// <param name="queueId"></param>
        /// <returns></returns>
        private async Task NotifyCalled(int queueId)
        {
            try
            {
                var calledEntry = await _context
                    .QueueEntries.Include(qe => qe.User)
                    .Include(qe => qe.Queue)
                    .Where(qe => qe.QueueId == queueId && qe.Status == "Notified")
                    .OrderBy(qe => qe.PositionNumber)
                    .FirstOrDefaultAsync();

                if (calledEntry == null)
                {
                    _logger.LogWarning("No called entry found for QueueId: {QueueId}", queueId);
                    return;
                }

                if (
                    string.IsNullOrEmpty(calledEntry.User?.PhoneNumber)
                    && string.IsNullOrEmpty(calledEntry.User?.Email)
                )
                {
                    _logger.LogWarning(
                        "User {UserId} has no phone number or email. Cannot send notification.",
                        calledEntry.UserId
                    );
                    return;
                }

                string message =
                    $"HiveQ: It's your turn at {calledEntry.Queue.QueueName}! Please proceed to the service counter.";

                _logger.LogInformation(
                    "Sending notification to UserId {UserId} (QueueEntryId: {QueueEntryId}): {Message}",
                    calledEntry.UserId,
                    calledEntry.QueueEntryId,
                    message
                );

                // Send notification (SMS or Email depending on what's available)
                await _smsService.SendNotificationAsync(
                    calledEntry.UserId,
                    calledEntry.QueueEntryId,
                    "Called",
                    message
                );

                _logger.LogInformation(
                    "Notification sent successfully to UserId {UserId}",
                    calledEntry.UserId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending called notifications for QueueId: {QueueId}",
                    queueId
                );
            }
        }

        /// <summary>
        /// Send SMS notifications to the top 3 users in the queue who have phone numbers
        /// </summary>
        private async Task NotifyTopUsersAsync(int queueId)
        {
            try
            {
                _logger.LogInformation(
                    "NotifyTopUsersAsync called for QueueId: {QueueId}",
                    queueId
                );

                // First, check ALL waiting users to see what data we have
                var allWaitingUsers = await _context
                    .QueueEntries.Include(qe => qe.User)
                    .Where(qe => qe.QueueId == queueId && qe.Status == "Waiting")
                    .ToListAsync();

                _logger.LogInformation(
                    "Total waiting users in queue {QueueId}: {Count}",
                    queueId,
                    allWaitingUsers.Count
                );

                foreach (var entry in allWaitingUsers)
                {
                    _logger.LogInformation(
                        "User {UserId} ({FirstName}): PhoneNumber='{PhoneNumber}', IsNullOrEmpty={IsEmpty}",
                        entry.UserId,
                        entry.User?.FirstName ?? "Unknown",
                        entry.User?.PhoneNumber ?? "NULL",
                        string.IsNullOrEmpty(entry.User?.PhoneNumber)
                    );
                }

                // Get top 3 waiting users with phone numbers
                var topUsers = await _context
                    .QueueEntries.Include(qe => qe.User)
                    .Include(qe => qe.Queue)
                    .Where(qe =>
                        qe.QueueId == queueId
                        && qe.Status == "Waiting"
                        && !string.IsNullOrEmpty(qe.User.PhoneNumber)
                    )
                    .OrderBy(qe => qe.PositionNumber)
                    .Take(3)
                    .ToListAsync();

                _logger.LogInformation(
                    "Found {Count} waiting users with phone numbers for QueueId: {QueueId}",
                    topUsers.Count,
                    queueId
                );

                if (topUsers.Count == 0)
                {
                    _logger.LogWarning(
                        "No waiting users with phone numbers found for QueueId: {QueueId}. SMS notifications skipped.",
                        queueId
                    );
                    return;
                }

                foreach (var entry in topUsers)
                {
                    _logger.LogInformation(
                        "Processing notification for UserId: {UserId}, QueueEntryId: {QueueEntryId}, PositionNumber: {PositionNumber}",
                        entry.UserId,
                        entry.QueueEntryId,
                        entry.PositionNumber
                    );

                    // Calculate current position in queue
                    var currentPosition = await _context
                        .QueueEntries.Where(qe =>
                            qe.QueueId == queueId
                            && (qe.Status == "Waiting" || qe.Status == "Notified")
                            && qe.PositionNumber <= entry.PositionNumber
                        )
                        .CountAsync();

                    _logger.LogInformation(
                        "Current position for UserId {UserId}: {CurrentPosition}",
                        entry.UserId,
                        currentPosition
                    );

                    string message;
                    if (currentPosition == 1)
                    {
                        message =
                            $"HiveQ: You're NEXT in line at {entry.Queue.QueueName}! Please be ready.";
                    }
                    else if (currentPosition == 2)
                    {
                        message =
                            $"HiveQ: You're #2 in line at {entry.Queue.QueueName}. You'll be called soon!";
                    }
                    else if (currentPosition == 3)
                    {
                        message =
                            $"HiveQ: You're #3 in line at {entry.Queue.QueueName}. Please prepare to be called.";
                    }
                    else
                    {
                        _logger.LogWarning(
                            "UserId {UserId} position {CurrentPosition} is not in top 3, skipping notification",
                            entry.UserId,
                            currentPosition
                        );
                        continue; // Skip if not in top 3
                    }

                    _logger.LogInformation(
                        "Sending SMS notification to UserId {UserId} at position {CurrentPosition}: {Message}",
                        entry.UserId,
                        currentPosition,
                        message
                    );

                    // Send SMS notification
                    await _smsService.SendNotificationAsync(
                        entry.UserId,
                        entry.QueueEntryId,
                        "QueueUpdate",
                        message
                    );

                    _logger.LogInformation(
                        "SMS notification sent successfully to UserId {UserId}",
                        entry.UserId
                    );
                }

                _logger.LogInformation(
                    "NotifyTopUsersAsync completed for QueueId: {QueueId}",
                    queueId
                );
            }
            catch (Exception ex)
            {
                // Log error but don't stop the queue operation
                _logger.LogError(
                    ex,
                    "Error sending SMS notifications for QueueId: {QueueId}",
                    queueId
                );
            }
        }
    }
}
