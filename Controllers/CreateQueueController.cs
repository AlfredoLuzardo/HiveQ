using Microsoft.AspNetCore.Mvc;

namespace HiveQ.Controllers
{
    public class CreateQueueController : Controller
    {
        // GET: CreateQueue
        public IActionResult Index()
        {
            return View();
        }

        // POST: CreateQueue
        [HttpPost]
        public IActionResult Index(string queueName)
        {
            // TODO: Implement queue creation logic
            TempData["Message"] = "Queue created successfully!";
            return RedirectToAction("CreateQueueConfirmation");
        }

        // GET: CreateQueue/CreateQueueConfirmation
        public IActionResult CreateQueueConfirmation()
        {
            return View();
        }
    }
}
