using Microsoft.EntityFrameworkCore;

namespace HiveQ.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Queue> Queues { get; set; }
        public DbSet<QueueEntry> QueueEntries { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<QueueHistory> QueueHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.CompanyName)
                .IsUnique();

            modelBuilder.Entity<Queue>()
                .HasIndex(q => q.QRCodeData)
                .IsUnique();

            // Configure relationships
            
            // User -> Company (One-to-One)
            modelBuilder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne(u => u.Company)
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Company -> Queues (One-to-Many)
            modelBuilder.Entity<Queue>()
                .HasOne(q => q.Company)
                .WithMany(c => c.Queues)
                .HasForeignKey(q => q.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Queue -> QueueEntries (One-to-Many)
            modelBuilder.Entity<QueueEntry>()
                .HasOne(qe => qe.Queue)
                .WithMany(q => q.QueueEntries)
                .HasForeignKey(qe => qe.QueueId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> QueueEntries (One-to-Many)
            modelBuilder.Entity<QueueEntry>()
                .HasOne(qe => qe.User)
                .WithMany(u => u.QueueEntries)
                .HasForeignKey(qe => qe.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // QueueEntry -> Notifications (One-to-Many)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.QueueEntry)
                .WithMany(qe => qe.Notifications)
                .HasForeignKey(n => n.QueueEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Notifications (One-to-Many)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // QueueEntry -> QueueHistory (One-to-One)
            modelBuilder.Entity<QueueHistory>()
                .HasOne(qh => qh.QueueEntry)
                .WithOne(qe => qe.QueueHistory)
                .HasForeignKey<QueueHistory>(qh => qh.QueueEntryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Queue -> QueueHistory (One-to-Many)
            modelBuilder.Entity<QueueHistory>()
                .HasOne(qh => qh.Queue)
                .WithMany(q => q.QueueHistories)
                .HasForeignKey(qh => qh.QueueId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> QueueHistory (One-to-Many)
            modelBuilder.Entity<QueueHistory>()
                .HasOne(qh => qh.User)
                .WithMany(u => u.QueueHistories)
                .HasForeignKey(qh => qh.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed some initial data for development (optional)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Use a FIXED date instead of DateTime.UtcNow to avoid migration conflicts
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed a test user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Email = "test@hiveq.com",
                    PasswordHash = "hashed_password_here", // In real app, use proper password hashing
                    PhoneNumber = "1234567890",
                    FirstName = "Test",
                    LastName = "User",
                    UserType = "CompanyOwner",
                    CreatedAt = seedDate,
                    IsActive = true
                }
            );

            // Seed a test company
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    CompanyId = 1,
                    UserId = 1,
                    CompanyName = "Sample Coffee Shop",
                    Description = "A cozy coffee shop in the heart of the city",
                    Address = "123 Main St, City, State 12345",
                    PhoneNumber = "5551234567",
                    Category = "Food & Beverage",
                    IsVerified = true,
                    CreatedAt = seedDate,
                    IsActive = true
                }
            );

            // Seed a test queue
            modelBuilder.Entity<Queue>().HasData(
                new Queue
                {
                    QueueId = 1,
                    CompanyId = 1,
                    QueueName = "Morning Service",
                    Description = "Main service queue for morning hours",
                    QRCodeData = "HIVEQ_QUEUE_1",
                    Status = "Active",
                    MaxCapacity = 50,
                    EstimatedWaitTimePerPerson = 5,
                    CurrentQueueSize = 0,
                    TotalServedToday = 0,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    IsActive = true
                }
            );
        }
    }
}
