using Microsoft.AspNetCore.Mvc;
using HiveQ.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace HiveQ.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// GET: Home/Index
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public IActionResult Index()
        {
            ViewBag.Name = HttpContext.User.Identity?.Name;
            return View();
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
