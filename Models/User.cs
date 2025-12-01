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

        // [Required]
        // [StringLength(50)]
        // public string UserType { get; set; } = "Customer"; // Customer or CompanyOwner

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public byte[]? ProfilePicture { get; set; }
        
        [StringLength(100)]
        public string? ProfilePictureContentType { get; set; }

        public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<QueueHistory> QueueHistories { get; set; } = new List<QueueHistory>();
        public ICollection<Queue> Queues { get; set; } = new List<Queue>();

    }
}
