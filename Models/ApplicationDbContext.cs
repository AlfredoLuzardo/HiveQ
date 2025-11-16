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

            modelBuilder.Entity<Queue>()
                .HasIndex(q => q.QRCodeData)
                .IsUnique();

            // Configure relationships
            
            // User -> Queues (One-to-Many)
            // modelBuilder.Entity<Queue>()
            //     .HasOne(q => q.User)
            //     .WithMany(u => u.Queues)
            //     .HasForeignKey(q => q.UserId)
            //     .OnDelete(DeleteBehavior.Cascade);

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

            // Seed a test user with company information
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Email = "owner@coffeeshop.com",
                    PasswordHash = "hashed_password_here", // In real app, use proper password hashing
                    PhoneNumber = "5551234567",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    CreatedAt = seedDate,
                    IsActive = true
                },
                new User
                {
                    UserId = 2,
                    Email = "customer@example.com",
                    PasswordHash = "hashed_password_here",
                    PhoneNumber = "5559876543",
                    FirstName = "John",
                    LastName = "Doe",
                    CreatedAt = seedDate,
                    IsActive = true
                }
            );

            // Seed a test queue
            modelBuilder.Entity<Queue>().HasData(
                new Queue
                {
                    QueueId = 1,
                    UserId = 1, // Created by Sarah (Coffee Shop owner)
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
