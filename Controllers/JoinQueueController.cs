using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HiveQ.Models;

namespace HiveQ.Controllers
{
    public class JoinQueueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JoinQueueController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: JoinQueue/{queueId} or JoinQueue?code={qrCodeData}
        // This is the page users land on when scanning the QR code
        public async Task<IActionResult> Index(int? queueId, string? code)
        {
            try
            {
                Queue? queue = null;

                // Find queue by ID or QR code
                if (queueId.HasValue)
                {
                    queue = await _context.Queues
                        .Include(q => q.QueueEntries)
                        .FirstOrDefaultAsync(q => q.QueueId == queueId.Value && q.IsActive);
                }
                else if (!string.IsNullOrEmpty(code))
                {
                    queue = await _context.Queues
                        .Include(q => q.QueueEntries)
                        .FirstOrDefaultAsync(q => q.QRCodeData == code && q.IsActive);
                }

                if (queue == null)
                {
                    TempData["Error"] = "Queue not found or is no longer active.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if queue is at capacity
                if (queue.CurrentQueueSize >= queue.MaxCapacity)
                {
                    TempData["Error"] = "This queue is currently at full capacity. Please try again later.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if queue is paused or closed
                if (queue.Status == "Paused" || queue.Status == "Closed")
                {
                    TempData["Error"] = $"This queue is currently {queue.Status.ToLower()}. Please check back later.";
                    return RedirectToAction("Index", "Home");
                }

                // Pass queue info to the view
                ViewBag.QueueId = queue.QueueId;
                ViewBag.QueueName = queue.QueueName;
                ViewBag.Description = queue.Description;
                ViewBag.CurrentSize = queue.CurrentQueueSize;
                ViewBag.EstimatedWaitTime = queue.CurrentQueueSize * queue.EstimatedWaitTimePerPerson;

                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while loading the queue.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: JoinQueue/Join
        [HttpPost]
        public async Task<IActionResult> Join(int queueId, string firstName, string lastName, 
            string? email, string? phoneNumber, string notificationPreference)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["Error"] = "First name and last name are required.";
                    return RedirectToAction("Index", new { queueId = queueId });
                }

                // Validate notification preference
                if (notificationPreference == "Email" && string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "Email is required for email notifications.";
                    return RedirectToAction("Index", new { queueId = queueId });
                }

                if ((notificationPreference == "SMS" || notificationPreference == "Both") 
                    && string.IsNullOrWhiteSpace(phoneNumber))
                {
                    TempData["Error"] = "Phone number is required for SMS notifications.";
                    return RedirectToAction("Index", new { queueId = queueId });
                }

                // Load the queue
                var queue = await _context.Queues
                    .Include(q => q.QueueEntries)
                    .FirstOrDefaultAsync(q => q.QueueId == queueId && q.IsActive);

                if (queue == null)
                {
                    TempData["Error"] = "Queue not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check capacity again
                if (queue.CurrentQueueSize >= queue.MaxCapacity)
                {
                    TempData["Error"] = "Queue is at full capacity.";
                    return RedirectToAction("Index", new { queueId = queueId });
                }

                // Create or find guest user
                User? user;
                var emailToCheck = email?.ToLower().Trim() ?? $"guest_{Guid.NewGuid()}@hiveq.local";
                
                // Check if user already exists (for registered users or returning guests)
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailToCheck);

                if (user == null)
                {
                    // Create a guest user
                    user = new User
                    {
                        Email = emailToCheck,
                        FirstName = firstName.Trim(),
                        LastName = lastName.Trim(),
                        PhoneNumber = phoneNumber?.Trim(),
                        PasswordHash = "GUEST_USER", // Placeholder - guest users can't log in
                        // IsVerified = false,
                        // CreatedAt = DateTime.UtcNow,
                        // IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(); // Save to get UserId
                }

                // Calculate position number (next available position)
                int positionNumber = queue.QueueEntries
                    .Where(qe => qe.Status == "Waiting" || qe.Status == "Notified")
                    .DefaultIfEmpty()
                    .Max(qe => qe?.PositionNumber ?? 0) + 1;

                // Calculate estimated wait time
                int estimatedWaitTime = (positionNumber - 1) * queue.EstimatedWaitTimePerPerson;

                // Create queue entry
                var queueEntry = new QueueEntry
                {
                    QueueId = queueId,
                    UserId = user.UserId,
                    PositionNumber = positionNumber,
                    Status = "Waiting",
                    JoinedAt = DateTime.UtcNow,
                    EstimatedWaitTime = estimatedWaitTime,
                    NotificationPreference = notificationPreference,
                    Notes = string.IsNullOrWhiteSpace(email) ? "Guest user (no email)" : null
                };

                _context.QueueEntries.Add(queueEntry);

                // Update queue statistics
                queue.CurrentQueueSize++;
                queue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Redirect to position view
                TempData["Message"] = "Successfully joined the queue!";
                return RedirectToAction("ViewPosition", new { queueEntryId = queueEntry.QueueEntryId });
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while joining the queue.";
                return RedirectToAction("Index", new { queueId = queueId });
            }
        }

        // GET: JoinQueue/ViewPosition/{queueEntryId}
        public async Task<IActionResult> ViewPosition(int queueEntryId)
        {
            try
            {
                var queueEntry = await _context.QueueEntries
                    .Include(qe => qe.Queue)
                    .Include(qe => qe.User)
                    .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId);

                if (queueEntry == null)
                {
                    TempData["Error"] = "Queue entry not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Calculate current position (in case people ahead left)
                var currentPosition = await _context.QueueEntries
                    .Where(qe => qe.QueueId == queueEntry.QueueId 
                        && (qe.Status == "Waiting" || qe.Status == "Notified")
                        && qe.PositionNumber <= queueEntry.PositionNumber)
                    .CountAsync();

                var peopleAhead = currentPosition - 1;
                var estimatedWait = peopleAhead * queueEntry.Queue.EstimatedWaitTimePerPerson;

                ViewBag.QueueEntry = queueEntry;
                ViewBag.CurrentPosition = currentPosition;
                ViewBag.PeopleAhead = peopleAhead;
                ViewBag.EstimatedWait = estimatedWait;

                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while loading your position.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: JoinQueue/Leave/{queueEntryId}
        [HttpPost]
        public async Task<IActionResult> Leave(int queueEntryId)
        {
            try
            {
                var queueEntry = await _context.QueueEntries
                    .Include(qe => qe.Queue)
                    .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId);

                if (queueEntry != null && (queueEntry.Status == "Waiting" || queueEntry.Status == "Notified"))
                {
                    queueEntry.Status = "Cancelled";
                    queueEntry.Queue.CurrentQueueSize--;
                    queueEntry.Queue.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["Message"] = "You have successfully left the queue.";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while leaving the queue.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
