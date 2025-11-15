# HiveQ Database - Quick Reference Guide

## Dependency Injection Setup

### In Controllers
Always inject the database context in your controller constructor:

```csharp
public class YourController : Controller
{
    private readonly ApplicationDbContext _context;

    public YourController(ApplicationDbContext context)
    {
        _context = context;
    }
}
```

## Common CRUD Operations

### CREATE (Add new records)

```csharp
// Add a new user
var user = new User
{
    Email = "user@example.com",
    PasswordHash = "hashed_password",
    FirstName = "John",
    LastName = "Doe",
    UserType = "Customer",
    PhoneNumber = "1234567890"
};
_context.Users.Add(user);
await _context.SaveChangesAsync();

// Add a new queue entry
var entry = new QueueEntry
{
    QueueId = queueId,
    UserId = userId,
    PositionNumber = 1,
    Status = "Waiting"
};
_context.QueueEntries.Add(entry);
await _context.SaveChangesAsync();
```

### READ (Query records)

```csharp
// Get all users
var users = await _context.Users.ToListAsync();

// Get user by ID
var user = await _context.Users.FindAsync(userId);

// Get user by email
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == email);

// Get active queues with company info
var queues = await _context.Queues
    .Include(q => q.Company)
    .Where(q => q.IsActive)
    .ToListAsync();

// Get queue entries with user and queue info
var entries = await _context.QueueEntries
    .Include(qe => qe.User)
    .Include(qe => qe.Queue)
    .Where(qe => qe.QueueId == queueId)
    .OrderBy(qe => qe.PositionNumber)
    .ToListAsync();

// Get user's queue entries
var myEntries = await _context.QueueEntries
    .Include(qe => qe.Queue)
        .ThenInclude(q => q.Company)
    .Where(qe => qe.UserId == userId)
    .ToListAsync();
```

### UPDATE (Modify records)

```csharp
// Update queue entry status
var entry = await _context.QueueEntries.FindAsync(entryId);
if (entry != null)
{
    entry.Status = "Notified";
    entry.NotifiedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}

// Update queue size
var queue = await _context.Queues.FindAsync(queueId);
if (queue != null)
{
    queue.CurrentQueueSize++;
    queue.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}

// Update company details
var company = await _context.Companies.FindAsync(companyId);
if (company != null)
{
    company.CompanyName = "New Name";
    company.Description = "Updated description";
    await _context.SaveChangesAsync();
}
```

### DELETE (Remove records)

```csharp
// Soft delete (recommended)
var queue = await _context.Queues.FindAsync(queueId);
if (queue != null)
{
    queue.IsActive = false;
    await _context.SaveChangesAsync();
}

// Hard delete
var notification = await _context.Notifications.FindAsync(notificationId);
if (notification != null)
{
    _context.Notifications.Remove(notification);
    await _context.SaveChangesAsync();
}
```

## Complex Queries

### Queue Dashboard Statistics
```csharp
public async Task<QueueDashboardViewModel> GetQueueDashboard(int companyId)
{
    var company = await _context.Companies
        .Include(c => c.Queues)
        .FirstOrDefaultAsync(c => c.CompanyId == companyId);

    var totalQueues = company?.Queues.Count(q => q.IsActive) ?? 0;
    
    var totalPeopleInQueue = await _context.QueueEntries
        .Where(qe => qe.Queue.CompanyId == companyId 
                  && qe.Status == "Waiting")
        .CountAsync();
    
    var totalServedToday = await _context.QueueEntries
        .Where(qe => qe.Queue.CompanyId == companyId 
                  && qe.ServedAt.HasValue
                  && qe.ServedAt.Value.Date == DateTime.UtcNow.Date)
        .CountAsync();

    return new QueueDashboardViewModel
    {
        TotalQueues = totalQueues,
        TotalPeopleInQueue = totalPeopleInQueue,
        TotalServedToday = totalServedToday
    };
}
```

### Get Queue Position for User
```csharp
public async Task<int?> GetUserPositionInQueue(int queueId, int userId)
{
    var entry = await _context.QueueEntries
        .Where(qe => qe.QueueId == queueId 
                  && qe.UserId == userId
                  && qe.Status == "Waiting")
        .FirstOrDefaultAsync();

    return entry?.PositionNumber;
}
```

### Archive Completed Queue Entry
```csharp
public async Task ArchiveQueueEntry(int queueEntryId)
{
    var entry = await _context.QueueEntries
        .Include(qe => qe.Queue)
        .FirstOrDefaultAsync(qe => qe.QueueEntryId == queueEntryId);

    if (entry != null && entry.ServedAt.HasValue)
    {
        var history = new QueueHistory
        {
            QueueId = entry.QueueId,
            UserId = entry.UserId,
            QueueEntryId = entry.QueueEntryId,
            JoinedAt = entry.JoinedAt,
            ServedAt = entry.ServedAt.Value,
            WaitTime = (int)(entry.ServedAt.Value - entry.JoinedAt).TotalMinutes,
            Status = entry.Status,
            Date = entry.ServedAt.Value.Date
        };

        _context.QueueHistories.Add(history);
        entry.Status = "Completed";
        
        await _context.SaveChangesAsync();
    }
}
```

### Get Company Analytics
```csharp
public async Task<CompanyAnalyticsViewModel> GetCompanyAnalytics(int companyId, DateTime startDate, DateTime endDate)
{
    var histories = await _context.QueueHistories
        .Where(qh => qh.Queue.CompanyId == companyId
                  && qh.Date >= startDate.Date
                  && qh.Date <= endDate.Date)
        .ToListAsync();

    return new CompanyAnalyticsViewModel
    {
        TotalCustomersServed = histories.Count,
        AverageWaitTime = histories.Any() ? histories.Average(h => h.WaitTime) : 0,
        PeakHours = histories.GroupBy(h => h.JoinedAt.Hour)
                           .OrderByDescending(g => g.Count())
                           .Take(3)
                           .Select(g => g.Key)
                           .ToList()
    };
}
```

## Transaction Management

### Using Transactions
For operations that must all succeed or fail together:

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Serve current customer
    var currentEntry = await _context.QueueEntries
        .FirstOrDefaultAsync(qe => qe.QueueId == queueId && qe.PositionNumber == 1);
    
    if (currentEntry != null)
    {
        currentEntry.Status = "Served";
        currentEntry.ServedAt = DateTime.UtcNow;
    }

    // Update all other positions
    var remainingEntries = await _context.QueueEntries
        .Where(qe => qe.QueueId == queueId && qe.PositionNumber > 1)
        .ToListAsync();
    
    foreach (var entry in remainingEntries)
    {
        entry.PositionNumber--;
    }

    // Update queue size
    var queue = await _context.Queues.FindAsync(queueId);
    if (queue != null)
    {
        queue.CurrentQueueSize--;
        queue.TotalServedToday++;
    }

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Efficient Queries

### Use AsNoTracking for Read-Only Queries
```csharp
// Faster for read-only data (reports, lists)
var queues = await _context.Queues
    .AsNoTracking()
    .Where(q => q.IsActive)
    .ToListAsync();
```

### Projection (Select only needed fields)
```csharp
// More efficient than loading full entities
var queueSummaries = await _context.Queues
    .Where(q => q.CompanyId == companyId)
    .Select(q => new
    {
        q.QueueId,
        q.QueueName,
        q.CurrentQueueSize,
        q.Status
    })
    .ToListAsync();
```

## Common Pitfalls to Avoid

### ❌ DON'T: Call SaveChanges in a loop
```csharp
// Bad - Multiple database round trips
foreach (var entry in entries)
{
    entry.Status = "Updated";
    await _context.SaveChangesAsync(); // Don't do this!
}
```

### ✅ DO: Batch updates
```csharp
// Good - Single database round trip
foreach (var entry in entries)
{
    entry.Status = "Updated";
}
await _context.SaveChangesAsync(); // Do this once
```

### ❌ DON'T: Use ToList() before filtering
```csharp
// Bad - Loads all data into memory first
var activeQueues = (await _context.Queues.ToListAsync())
    .Where(q => q.IsActive)
    .ToList();
```

### ✅ DO: Filter in the database
```csharp
// Good - Filtering happens in database
var activeQueues = await _context.Queues
    .Where(q => q.IsActive)
    .ToListAsync();
```

## Connection String Notes

### Development (Current Setup)
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=hiveq.db"
}
```
This creates `hiveq.db` in the project root directory.

### Alternative Locations
```json
// In a Data folder
"DefaultConnection": "Data Source=Data/hiveq.db"

// Absolute path
"DefaultConnection": "Data Source=/path/to/database/hiveq.db"
```

## Error Handling

### Example Error Handler
```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    // Handle database update errors
    if (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
    {
        ModelState.AddModelError("", "A record with this value already exists.");
    }
    else
    {
        ModelState.AddModelError("", "An error occurred while saving.");
    }
}
catch (Exception ex)
{
    // Log the exception
    ModelState.AddModelError("", "An unexpected error occurred.");
}
```

## Testing Database Operations

### In-Memory Database for Testing
Add to your test project:
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

using var context = new ApplicationDbContext(options);
// Run your tests
```

## Useful Extensions

### Create a helper method for pagination
```csharp
public static async Task<List<T>> PaginateAsync<T>(
    this IQueryable<T> query, 
    int page, 
    int pageSize)
{
    return await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}

// Usage
var queues = await _context.Queues
    .Where(q => q.IsActive)
    .PaginateAsync(page: 1, pageSize: 10);
```
