using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HiveQ.Models;
using HiveQ.Services;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace HiveQ.Controllers
{
    public class CreateQueueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authService;

        public CreateQueueController(ApplicationDbContext context, AuthenticationService authService)
        {
            _context = context;
            _authService = authService;
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
        public async Task<IActionResult> Index(string queueName, string? description, int? maxCapacity, 
            int estimatedTime, int maxPartySize, string status, bool emailNotifications, bool smsNotifications)
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

                if (maxPartySize <= 0 || maxPartySize > 50)
                {
                    TempData["Error"] = "Maximum party size must be between 1 and 50.";
                    return View();
                }

                // Get current user
                var currentUser = await _authService.GetCurrentUserAsync(User);
                if (currentUser == null)
                {
                    TempData["Error"] = "User authentication error. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Generate unique QR code data (using queue name + timestamp)
                string qrCodeData = $"{queueName}_{DateTime.UtcNow.Ticks}";

                // Create new queue object
                var newQueue = new Queue
                {
                    UserId = currentUser.UserId,
                    QueueName = queueName,
                    Description = description,
                    QRCodeData = qrCodeData,
                    Status = status ?? "Open",
                    MaxCapacity = maxCapacity ?? 100,
                    EstimatedWaitTimePerPerson = estimatedTime,
                    MaxPartySize = maxPartySize,
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
            if (TempData["QueueId"] == null || TempData["QRCodeData"] == null)
            {
                return RedirectToAction("Index");
            }

            // Generate QR code for the join URL
            string qrCodeData = TempData["QRCodeData"]?.ToString() ?? "";
            string joinUrl = $"{Request.Scheme}://{Request.Host}/JoinQueue?code={qrCodeData}";
            
            // Generate QR code image
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeDataObj = qrGenerator.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeDataObj))
                {
                    using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            qrCodeImage.Save(ms, ImageFormat.Png);
                            byte[] qrCodeBytes = ms.ToArray();
                            string qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                            ViewBag.QRCodeImage = $"data:image/png;base64,{qrCodeBase64}";
                        }
                    }
                }
            }

            // Keep data for the view
            TempData.Keep("QueueName");
            TempData.Keep("QueueId");
            TempData.Keep("QRCodeData");

            return View();
        }
    }
}
