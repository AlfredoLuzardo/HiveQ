using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HiveQ.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Company/Business Information (optional - only if user creates queues)
        [StringLength(200)]
        public string? CompanyName { get; set; }

        public string? CompanyDescription { get; set; }

        [StringLength(500)]
        public string? CompanyAddress { get; set; }

        [StringLength(100)]
        public string? CompanyCategory { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Queue> Queues { get; set; } = new List<Queue>();
        public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<QueueHistory> QueueHistories { get; set; } = new List<QueueHistory>();
    }
}
