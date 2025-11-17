using HiveQ.Models;
using Microsoft.EntityFrameworkCore;

namespace HiveQ.Services
{
    public class GuestUserCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GuestUserCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily

        public GuestUserCleanupService(
            IServiceProvider serviceProvider,
            ILogger<GuestUserCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Guest User Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupGuestUsersAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during guest user cleanup.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour
                }
            }
        }

        private async Task CleanupGuestUsersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Find guest users with no active queue entries
            var inactiveGuestUsers = await context.Users
                .Where(u => u.PasswordHash == "GUEST_USER")
                .Where(u => !context.QueueEntries
                    .Any(qe => qe.UserId == u.UserId && 
                              (qe.Status == "Waiting" || qe.Status == "Notified")))
                .ToListAsync();

            if (inactiveGuestUsers.Any())
            {
                _logger.LogInformation($"Cleaning up {inactiveGuestUsers.Count} inactive guest users.");
                
                context.Users.RemoveRange(inactiveGuestUsers);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Successfully cleaned up {inactiveGuestUsers.Count} guest users.");
            }
            else
            {
                _logger.LogInformation("No inactive guest users to clean up.");
            }
        }
    }
}
