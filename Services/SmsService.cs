using HiveQ.Models;
using Microsoft.EntityFrameworkCore;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace HiveQ.Services
{
    /// <summary>
    /// Interface for SMS Service
    /// </summary>
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task SendNotificationAsync(
            int userId,
            int queueEntryId,
            string notificationType,
            string message
        );
    }

    public class SmsService : ISmsService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmsService> _logger;

        public SmsService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<SmsService> logger
        )
        {
            _accountSid =
                configuration["Twilio:AccountSid"]
                ?? throw new InvalidOperationException("Twilio AccountSid is not configured.");
            _authToken =
                configuration["Twilio:AuthToken"]
                ?? throw new InvalidOperationException("Twilio AuthToken is not configured.");
            _fromPhoneNumber =
                configuration["Twilio:PhoneNumber"]
                ?? throw new InvalidOperationException("Twilio PhoneNumber is not configured.");

            _context = context;
            _logger = logger;

            // Initialize Twilio client
            TwilioClient.Init(_accountSid, _authToken);
        }

        /// <summary>
        /// Send an SMS message to a phone number using Twilio
        /// </summary>
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Ensure phone number is in E.164 format (e.g., +1234567890)
                if (!phoneNumber.StartsWith("+"))
                {
                    _logger.LogWarning(
                        "Phone number {PhoneNumber} is not in E.164 format",
                        phoneNumber
                    );
                    return false;
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_fromPhoneNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                if (
                    messageResource.Status == MessageResource.StatusEnum.Failed
                    || messageResource.Status == MessageResource.StatusEnum.Undelivered
                )
                {
                    _logger.LogError(
                        "Failed to send SMS to {PhoneNumber}. Status: {Status}, ErrorCode: {ErrorCode}",
                        phoneNumber,
                        messageResource.Status,
                        messageResource.ErrorCode
                    );
                    return false;
                }

                _logger.LogInformation(
                    "SMS sent successfully to {PhoneNumber}. MessageSid: {MessageSid}",
                    phoneNumber,
                    messageResource.Sid
                );
                return true;
            }
            catch (Twilio.Exceptions.ApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Twilio API error sending SMS to {PhoneNumber}. Code: {Code}, Message: {Message}",
                    phoneNumber,
                    ex.Code,
                    ex.Message
                );
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        /// <summary>
        /// Send a notification via SMS if the user has a phone number, and save to database
        /// </summary>
        public async Task SendNotificationAsync(
            int userId,
            int queueEntryId,
            string notificationType,
            string message
        )
        {
            try
            {
                // Get user with phone number
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return;
                }

                // Create notification record
                var notification = new Notification
                {
                    UserId = userId,
                    QueueEntryId = queueEntryId,
                    Type = notificationType,
                    Channel = string.IsNullOrEmpty(user.PhoneNumber) ? "Email" : "SMS",
                    Message = message,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send SMS if user has phone number
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    bool success = await SendSmsAsync(user.PhoneNumber, message);

                    notification.Status = success ? "Sent" : "Failed";
                    notification.SentAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation(
                        "User {UserId} has no phone number. SMS not sent.",
                        userId
                    );
                    notification.Status = "Skipped";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }
    }
}
