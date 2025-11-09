using Microsoft.AspNetCore.Mvc;

namespace HiveQ.Controllers
{
    public class ProfileController : Controller
    {
        // GET: Profile
        public IActionResult Index()
        {
            // TODO: Load user profile from database
            return View();
        }

        // POST: Profile/DeleteAccount
        [HttpPost]
        public IActionResult DeleteAccount()
        {
            // TODO: Implement account deletion logic
            TempData["Message"] = "Account deleted successfully";
            return RedirectToAction("Index", "Home");
        }

        // POST: Profile/SubmitProfilePic
        [HttpPost]
        public IActionResult SubmitProfilePic(IFormFile profilePicture)
        {
            // TODO: Implement profile picture upload logic
            TempData["Message"] = "Profile picture updated successfully";
            return RedirectToAction("Index");
        }
    }
}
