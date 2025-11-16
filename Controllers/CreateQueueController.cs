using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveQ.Controllers
{
    public class CreateQueueController : Controller
    {
        // GET: CreateQueue
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        // POST: CreateQueue
        [HttpPost]
        [Authorize]
        public IActionResult Index(string queueName)
        {
            // TODO: Implement queue creation logic
            TempData["Message"] = "Queue created successfully!";
            return RedirectToAction("CreateQueueConfirmation");
        }

        // GET: CreateQueue/CreateQueueConfirmation
        [Authorize]
        public IActionResult CreateQueueConfirmation()
        {
            return View();
        }
    }
}
