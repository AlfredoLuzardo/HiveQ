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

        // GET: JoinQueue?code={qrCodeData}
        // This is the page users land on when scanning the QR code
        // Security: Only accepts encrypted QR code, not direct queueId
        public async Task<IActionResult> Index(string? code)
        {
            try
            {
                Queue? queue = null;

                // Find queue by QR code only (security measure)
                if (!string.IsNullOrEmpty(code))
                {
                    queue = await _context.Queues
                        .Include(q => q.QueueEntries)
                        .FirstOrDefaultAsync(q => q.QRCodeData == code && q.IsActive);
                }
                else
                {
                    TempData["Error"] = "Invalid queue access. Please scan the QR code.";
                    return RedirectToAction("Index", "Home");
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

                // If user is authenticated, auto-join them
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                        if (currentUser != null)
                        {
                            // Check if user is already in this queue
                            var existingEntry = await _context.QueueEntries
                                .FirstOrDefaultAsync(qe => qe.QueueId == queue.QueueId && 
                                                          qe.UserId == currentUser.UserId && 
                                                          (qe.Status == "Waiting" || qe.Status == "Notified"));

                            if (existingEntry != null)
                            {
                                // User already in queue, redirect to position page
                                return RedirectToAction("ViewPosition", new { queueEntryId = existingEntry.QueueEntryId });
                            }

                            // Auto-join the authenticated user
                            return await JoinAuthenticatedUser(queue, currentUser);
                        }
                    }
                }

                // Pass queue info to the view for guest users
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
        public async Task<IActionResult> Join(int queueId, string? firstName, string? lastName, 
            string? email, string? phoneNumber, string? notificationPreference)
        {
            try
            {
                // Check if user is authenticated
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                        var userQueue = await _context.Queues
                            .Include(q => q.QueueEntries)
                            .FirstOrDefaultAsync(q => q.QueueId == queueId && q.IsActive);

                        if (currentUser != null && userQueue != null)
                        {
                            return await JoinAuthenticatedUser(userQueue, currentUser);
                        }
                    }
                }

                // Guest user validation
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["Error"] = "First name and last name are required.";
                    // Redirect back using queue's QR code for security
                    var queueForRedirect = await _context.Queues.FindAsync(queueId);
                    return RedirectToAction("Index", new { code = queueForRedirect?.QRCodeData });
                }

                notificationPreference = notificationPreference ?? "None";

                // Validate notification preference
                if (notificationPreference == "Email" && string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "Email is required for email notifications.";
                    var queueForRedirect1 = await _context.Queues.FindAsync(queueId);
                    return RedirectToAction("Index", new { code = queueForRedirect1?.QRCodeData });
                }

                if ((notificationPreference == "SMS" || notificationPreference == "Both") 
                    && string.IsNullOrWhiteSpace(phoneNumber))
                {
                    TempData["Error"] = "Phone number is required for SMS notifications.";
                    var queueForRedirect2 = await _context.Queues.FindAsync(queueId);
                    return RedirectToAction("Index", new { code = queueForRedirect2?.QRCodeData });
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
                    return RedirectToAction("Index", new { code = queue.QRCodeData });
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
                return RedirectToAction("Index", "Home");
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
                    .Include(qe => qe.User)
                    .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId);

                if (queueEntry != null && (queueEntry.Status == "Waiting" || queueEntry.Status == "Notified"))
                {
                    queueEntry.Status = "Cancelled";
                    queueEntry.Queue.CurrentQueueSize--;
                    queueEntry.Queue.UpdatedAt = DateTime.UtcNow;

                    // Auto-delete guest user if they have no other active queue entries
                    if (queueEntry.User != null && queueEntry.User.PasswordHash == "GUEST_USER")
                    {
                        var hasOtherActiveEntries = await _context.QueueEntries
                            .AnyAsync(qe => qe.UserId == queueEntry.UserId &&
                                           qe.QueueEntryId != queueEntryId &&
                                           (qe.Status == "Waiting" || qe.Status == "Notified"));

                        if (!hasOtherActiveEntries)
                        {
                            _context.Users.Remove(queueEntry.User);
                        }
                    }

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

        // POST: JoinQueue/MarkArrived/{queueEntryId}
        [HttpPost]
        public async Task<IActionResult> MarkArrived(int queueEntryId)
        {
            try
            {
                var queueEntry = await _context.QueueEntries
                    .Include(qe => qe.Queue)
                    .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId);

                if (queueEntry != null && queueEntry.Status == "Notified")
                {
                    // Update notes to indicate customer has arrived
                    queueEntry.Notes = (queueEntry.Notes ?? "") + " | Customer arrived at " + DateTime.UtcNow.ToLocalTime().ToString("h:mm tt");
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Thank you! The staff has been notified that you've arrived.";
                }

                return RedirectToAction("ViewPosition", new { queueEntryId = queueEntryId });
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred.";
                return RedirectToAction("ViewPosition", new { queueEntryId = queueEntryId });
            }
        }

        // GET: JoinQueue/MyQueues
        public async Task<IActionResult> MyQueues()
        {
            try
            {
                List<QueueEntry> myEntries;

                if (User.Identity?.IsAuthenticated == true)
                {
                    // Authenticated user - get their queue entries by email
                    var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        return View(new List<QueueEntry>());
                    }

                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                    if (currentUser == null)
                    {
                        return View(new List<QueueEntry>());
                    }

                    myEntries = await _context.QueueEntries
                        .Include(qe => qe.Queue)
                        .Include(qe => qe.User)
                        .Where(qe => qe.UserId == currentUser.UserId && 
                                   (qe.Status == "Waiting" || qe.Status == "Notified"))
                        .OrderBy(qe => qe.JoinedAt)
                        .ToListAsync();
                }
                else
                {
                    // Guest user - show empty (they need to bookmark individual queue positions)
                    myEntries = new List<QueueEntry>();
                }

                return View(myEntries);
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while loading your queues.";
                return View(new List<QueueEntry>());
            }
        }

        // Helper method to join authenticated users
        private async Task<IActionResult> JoinAuthenticatedUser(Queue queue, User user)
        {
            try
            {
                // Check capacity
                if (queue.CurrentQueueSize >= queue.MaxCapacity)
                {
                    TempData["Error"] = "Queue is at full capacity.";
                    return RedirectToAction("Index", new { code = queue.QRCodeData });
                }

                // Calculate position number
                int positionNumber = queue.QueueEntries
                    .Where(qe => qe.Status == "Waiting" || qe.Status == "Notified")
                    .DefaultIfEmpty()
                    .Max(qe => qe?.PositionNumber ?? 0) + 1;

                // Calculate estimated wait time
                int estimatedWaitTime = (positionNumber - 1) * queue.EstimatedWaitTimePerPerson;

                // Create queue entry
                var queueEntry = new QueueEntry
                {
                    QueueId = queue.QueueId,
                    UserId = user.UserId,
                    PositionNumber = positionNumber,
                    Status = "Waiting",
                    JoinedAt = DateTime.UtcNow,
                    EstimatedWaitTime = estimatedWaitTime,
                    NotificationPreference = !string.IsNullOrEmpty(user.Email) && !user.Email.Contains("@hiveq.local") ? "Email" : "None",
                    Notes = "Authenticated user"
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
                return RedirectToAction("Index", new { code = queue.QRCodeData });
            }
        }
    }
}
