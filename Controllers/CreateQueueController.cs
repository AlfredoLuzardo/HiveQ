using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HiveQ.Models;

namespace HiveQ.Controllers
{
    public class CreateQueueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CreateQueueController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CreateQueue
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        // POST: CreateQueue
        [HttpPost]
        [Authorize]
        public IActionResult Index(string queueName, string? description, int? maxCapacity, 
            int estimatedTime, string status, bool emailNotifications, bool smsNotifications)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(queueName))
                {
                    TempData["Error"] = "Queue name is required.";
                    return View();
                }

                if (estimatedTime <= 0)
                {
                    TempData["Error"] = "Estimated time per person must be greater than 0.";
                    return View();
                }

                // TODO: Get actual logged-in user ID from session/authentication
                // For now, using a default user ID of 1
                int userId = 1;

                // Generate unique QR code data (using queue name + timestamp)
                string qrCodeData = $"{queueName}_{DateTime.UtcNow.Ticks}";

                // Create new queue object
                var newQueue = new Queue
                {
                    UserId = userId,
                    QueueName = queueName,
                    Description = description,
                    QRCodeData = qrCodeData,
                    Status = status ?? "Open",
                    MaxCapacity = maxCapacity ?? 100,
                    EstimatedWaitTimePerPerson = estimatedTime,
                    CurrentQueueSize = 0,
                    TotalServedToday = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Save to database
                _context.Queues.Add(newQueue);
                _context.SaveChanges();

                // Store success message and queue details for confirmation page
                TempData["Message"] = "Queue created successfully!";
                TempData["QueueName"] = queueName;
                TempData["QueueId"] = newQueue.QueueId;
                TempData["QRCodeData"] = qrCodeData;

                return RedirectToAction("CreateQueueConfirmation");
            }
            catch (Exception ex)
            {
                // Log the error (in production, use proper logging)
                TempData["Error"] = $"An error occurred while creating the queue: {ex.Message}";
                return View();
            }
        }

        // GET: CreateQueue/CreateQueueConfirmation
        [Authorize]
        public IActionResult CreateQueueConfirmation()
        {
            // Check if we have the necessary data from queue creation
            if (TempData["QueueId"] == null)
            {
                return RedirectToAction("Index");
            }

            return View();
        }
    }
}
