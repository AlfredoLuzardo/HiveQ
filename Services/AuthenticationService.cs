using System.Security.Claims;
using HiveQ.Models;
using Microsoft.EntityFrameworkCore;

namespace HiveQ.Services
{
    /// <summary>
    /// Service to handle common authentication operations across controllers
    /// </summary>
    public class AuthenticationService
    {
        private readonly ApplicationDbContext _context;

        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the currently authenticated user from claims
        /// </summary>
        /// <param name="user">ClaimsPrincipal from controller</param>
        /// <returns>User entity or null if not found</returns>
        public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            var email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Gets current user's email from claims
        /// </summary>
        public string? GetCurrentUserEmail(ClaimsPrincipal user)
        {
            return user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }
    }
}
