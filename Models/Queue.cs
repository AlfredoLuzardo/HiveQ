using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HiveQ.Models
{
    public class Queue
    {
        [Key]
        public int QueueId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string QueueName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(500)]
        public string QRCodeData { get; set; } = string.Empty;

        [StringLength(500)]
        public string? QRCodeImageUrl { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Paused, Closed

        public int MaxCapacity { get; set; } = 100;

        public int EstimatedWaitTimePerPerson { get; set; } = 5; // in minutes

        public int MaxPartySize { get; set; } = 1; // Maximum people allowed per queue position

        public int CurrentQueueSize { get; set; } = 0;

        public int TotalServedToday { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
        public ICollection<QueueHistory> QueueHistories { get; set; } = new List<QueueHistory>();
    }
}
