using Microsoft.AspNetCore.Mvc;

namespace HiveQ.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // TODO: Implement login logic
            TempData["Message"] = "Login functionality - Coming soon!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        public IActionResult Register(string email, string password)
        {
            // TODO: Implement registration logic
            TempData["Message"] = "Registration functionality - Coming soon!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            // TODO: Implement logout logic
            TempData["Message"] = "Logged out successfully";
            return RedirectToAction("Index", "Home");
        }
    }
}
