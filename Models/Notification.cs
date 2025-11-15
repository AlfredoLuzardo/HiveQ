using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HiveQ.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [ForeignKey("QueueEntry")]
        public int QueueEntryId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // QueueUpdate, YourTurn, Reminder

        [Required]
        [StringLength(50)]
        public string Channel { get; set; } = string.Empty; // SMS, Email, Push

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Delivered

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public QueueEntry QueueEntry { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
