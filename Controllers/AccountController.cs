using System.Security.Claims;
using HiveQ.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HiveQ.Controllers
{
    /// <summary>
    /// Controller for Account related actions like Login, Logout, Register
    /// </summary>
    /// <remarks>
    /// Constructor for AccountController
    /// </remarks>
    /// <param name="signInManager"></param>
    /// <param name="userManager"></param>
    public class AccountController(
        ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// GET: Account/Login
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// POST: Account/Login
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.Where(u => u.Email == model.Email && u.PasswordHash == model.Password).FirstOrDefault();
                
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new("FirstName", user.FirstName),
                        new("LastName", user.LastName),
                        new(ClaimTypes.Email, user.Email),
                        new(ClaimTypes.Role, "User")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            return View(model);
        }

        /// <summary>
        /// GET: Account/Logout
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        /// <summary>
        /// GET: Account/Register
        /// </summary>
        /// <returns></returns>
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// POST: Account/Register
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = model.Password,
                    PhoneNumber = model.PhoneNumber
                };

                try
                {
                    _context.Users.Add(user);
                    _context.SaveChanges();

                    ModelState.Clear();
                    ViewBag.Message = $"{user.FirstName} {user.LastName} registered successfully. Please log in.";    
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while registering the user.");
                    return View(model);
                }

                return View();
            }
            return View(model);
        }
    }
}
