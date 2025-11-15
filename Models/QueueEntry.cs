using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HiveQ.Models
{
    public class QueueEntry
    {
        [Key]
        public int QueueEntryId { get; set; }

        [Required]
        [ForeignKey("Queue")]
        public int QueueId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public int PositionNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Waiting"; // Waiting, Notified, Served, Cancelled, NoShow

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? NotifiedAt { get; set; }

        public DateTime? ServedAt { get; set; }

        public int EstimatedWaitTime { get; set; } = 0; // in minutes

        [StringLength(50)]
        public string NotificationPreference { get; set; } = "SMS"; // SMS, Email, Both

        public string? Notes { get; set; }

        // Navigation properties
        public Queue Queue { get; set; } = null!;
        public User User { get; set; } = null!;
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public QueueHistory? QueueHistory { get; set; }
    }
}
