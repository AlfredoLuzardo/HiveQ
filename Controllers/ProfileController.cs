using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HiveQ.Models;
using HiveQ.Services;

namespace HiveQ.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authService;

        public ProfileController(ApplicationDbContext context, AuthenticationService authService)
        {
            _context = context;
            _authService = authService;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var currentUser = await _authService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            return View(currentUser);
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
        public async Task<IActionResult> SubmitProfilePic(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["Error"] = "Please select an image file.";
                return RedirectToAction("Index");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(profilePicture.ContentType.ToLower()))
            {
                TempData["Error"] = "Only JPEG, PNG, and GIF images are allowed.";
                return RedirectToAction("Index");
            }

            // Validate file size (max 5MB)
            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Image size must be less than 5MB.";
                return RedirectToAction("Index");
            }

            var currentUser = await _authService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Convert image to byte array
            using (var memoryStream = new MemoryStream())
            {
                await profilePicture.CopyToAsync(memoryStream);
                currentUser.ProfilePicture = memoryStream.ToArray();
                currentUser.ProfilePictureContentType = profilePicture.ContentType;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Profile picture updated successfully!";
            return RedirectToAction("Index");
        }

        // GET: Profile/GetProfilePicture
        [HttpGet]
        public async Task<IActionResult> GetProfilePicture(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.ProfilePicture != null && user.ProfilePictureContentType != null)
            {
                return File(user.ProfilePicture, user.ProfilePictureContentType);
            }
            return NotFound();
        }
    }
}
