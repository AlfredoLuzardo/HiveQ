using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HiveQ.Models
{
    public class QueueHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        [ForeignKey("Queue")]
        public int QueueId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("QueueEntry")]
        public int? QueueEntryId { get; set; }

        public DateTime JoinedAt { get; set; }

        public DateTime? ServedAt { get; set; }

        public int WaitTime { get; set; } = 0; // in minutes

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // Completed, Cancelled, NoShow

        public DateTime Date { get; set; }

        // Navigation properties
        public Queue Queue { get; set; } = null!;
        public User User { get; set; } = null!;
        public QueueEntry? QueueEntry { get; set; }
    }
}
