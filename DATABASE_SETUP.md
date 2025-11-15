# HiveQ Database Setup Guide

## Overview
This guide will help you set up and configure the SQLite database for your HiveQ project. The database has been designed based on your ER diagram with all necessary entities and relationships.

## Database Structure

### Entities Created:
1. **Users** - Stores user accounts (customers and company owners)
2. **Companies** - Stores company information
3. **Queues** - Stores queue configurations for companies
4. **QueueEntries** - Tracks individual customer queue entries
5. **Notifications** - Manages all notifications sent to users
6. **QueueHistory** - Archives completed queue entries for analytics

## Setup Instructions

### Step 1: Restore NuGet Packages
Open a terminal in your HiveQ project directory and run:
```bash
dotnet restore
```

This will download all required Entity Framework Core packages.

### Step 2: Create Initial Migration
Create the initial database migration with:
```bash
dotnet ef migrations add InitialCreate
```

This creates a migration file that defines your database schema.

### Step 3: Apply Migration to Database
Apply the migration to create the database:
```bash
dotnet ef database update
```

This will create a `hiveq.db` SQLite database file in your project root.

### Step 4: Verify Database Creation
You should see a `hiveq.db` file in your project directory. You can view it using:
- SQLite Browser (https://sqlitebrowser.org/)
- VS Code SQLite extension
- Azure Data Studio with SQLite extension

## What Was Added to Your Project

### 1. Entity Models (Models folder)
- **User.cs** - User entity with authentication and profile info
- **Company.cs** - Company entity linked to user accounts
- **Queue.cs** - Queue entity with QR code and capacity management
- **QueueEntry.cs** - Individual queue entry tracking
- **Notification.cs** - Notification management
- **QueueHistory.cs** - Historical queue data for analytics

### 2. Database Context
- **ApplicationDbContext.cs** - Main database context managing all entities
  - Configured all relationships (one-to-one, one-to-many)
  - Set up unique constraints for Email, CompanyName, and QRCodeData
  - Includes seed data for testing (1 user, 1 company, 1 queue)

### 3. Configuration Files Updated
- **HiveQ.csproj** - Added Entity Framework Core packages:
  - Microsoft.EntityFrameworkCore.Sqlite (9.0.0)
  - Microsoft.EntityFrameworkCore.Design (9.0.0)
  - Microsoft.EntityFrameworkCore.Tools (9.0.0)

- **Program.cs** - Added database service registration:
  ```csharp
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
  ```

- **appsettings.json** - Added connection string:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=hiveq.db"
  }
  ```

## Database Relationships Explained

### One-to-One
- **User → Company**: Each company owner (user) can create one company

### One-to-Many
- **Company → Queues**: A company can have multiple queues
- **Queue → QueueEntries**: A queue contains multiple entries
- **User → QueueEntries**: A user can join multiple queues
- **QueueEntry → Notifications**: Each queue entry can trigger multiple notifications
- **User → Notifications**: A user can receive multiple notifications
- **Queue → QueueHistory**: A queue has historical records
- **User → QueueHistory**: A user has historical queue records

### Special Relationships
- **QueueEntry ↔ QueueHistory**: One-to-one relationship (optional)
  When a queue entry is completed, it can be archived to history

## Seed Data Included

For testing purposes, the database will be initialized with:

1. **Test User**
   - Email: test@hiveq.com
   - Type: CompanyOwner
   - Name: Test User

2. **Sample Company**
   - Name: Sample Coffee Shop
   - Category: Food & Beverage
   - Status: Verified

3. **Sample Queue**
   - Name: Morning Service
   - Capacity: 50 people
   - Wait time: 5 minutes per person
   - Status: Active
   - QR Code: HIVEQ_QUEUE_1

## Using the Database in Controllers

### Example: Getting all active queues for a company
```csharp
public class ManageQueuesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ManageQueuesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var queues = await _context.Queues
            .Include(q => q.Company)
            .Where(q => q.IsActive)
            .ToListAsync();
        
        return View(queues);
    }
}
```

### Example: Adding a new queue entry
```csharp
public async Task<IActionResult> JoinQueue(int queueId, int userId)
{
    var queue = await _context.Queues.FindAsync(queueId);
    
    if (queue == null || queue.CurrentQueueSize >= queue.MaxCapacity)
    {
        return BadRequest("Queue not available");
    }

    var entry = new QueueEntry
    {
        QueueId = queueId,
        UserId = userId,
        PositionNumber = queue.CurrentQueueSize + 1,
        Status = "Waiting",
        JoinedAt = DateTime.UtcNow,
        EstimatedWaitTime = queue.EstimatedWaitTimePerPerson * queue.CurrentQueueSize
    };

    _context.QueueEntries.Add(entry);
    queue.CurrentQueueSize++;
    
    await _context.SaveChangesAsync();
    
    return Ok(entry);
}
```

## Common EF Core Commands

### Create a new migration
```bash
dotnet ef migrations add MigrationName
```

### Apply migrations
```bash
dotnet ef database update
```

### Rollback to a specific migration
```bash
dotnet ef database update PreviousMigrationName
```

### Remove last migration (if not applied)
```bash
dotnet ef migrations remove
```

### Generate SQL script from migrations
```bash
dotnet ef migrations script
```

### Drop database (careful!)
```bash
dotnet ef database drop
```

## Troubleshooting

### Issue: "No database provider has been configured"
**Solution**: Make sure Program.cs has the DbContext configuration and your connection string is in appsettings.json

### Issue: "A connection was successfully established with the server, but..."
**Solution**: Check your connection string format in appsettings.json

### Issue: Migration files not being created
**Solution**: Ensure Microsoft.EntityFrameworkCore.Design package is installed

### Issue: "The type or namespace name 'ApplicationDbContext' could not be found"
**Solution**: Check that the using statement `using HiveQ.Models;` is at the top of Program.cs

## Next Steps

After setting up the database, you can:

1. **Update Controllers** to use the database context for CRUD operations
2. **Implement Authentication** using the User entity
3. **Create Company Registration** flow
4. **Build Queue Management** features
5. **Implement QR Code Generation** for queues
6. **Add Real-time Updates** for queue positions
7. **Create Analytics Dashboard** using QueueHistory data

## Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [ASP.NET Core Data Access](https://docs.microsoft.com/en-us/aspnet/core/data/)

## Database Schema Visualization

```
Users (1) ──── (1) Companies (1) ──── (Many) Queues
  │                                        │
  │                                        │
  │                                   (Many)│
  │                                        │
  └────── (Many) QueueEntries ────────────┘
              │         │
              │         │
         (Many)│         └── (1) QueueHistory
              │
              │
         Notifications
```

## Notes

- All timestamps are stored in UTC
- IsActive flags allow for soft deletes
- Status fields use string enums (consider converting to actual enums later)
- Foreign key relationships use Cascade, Restrict, or SetNull based on business logic
- Unique constraints prevent duplicate emails, company names, and QR codes
