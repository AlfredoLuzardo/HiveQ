# HiveQ Database Integration - Step-by-Step Checklist

## ✅ Pre-Setup Verification

- [ ] I have Visual Studio, VS Code, or another .NET IDE installed
- [ ] I have .NET 9.0 SDK installed (`dotnet --version` shows 9.0.x)
- [ ] I have downloaded/extracted the updated HiveQ project
- [ ] I've read the DATABASE_INTEGRATION_SUMMARY.md file

## 📥 Step 1: Extract and Open Project

- [ ] Extract the HiveQ.zip file (if you downloaded it)
- [ ] Open the project in your IDE
- [ ] Verify all new files are present in Models folder:
  - [ ] User.cs
  - [ ] Company.cs
  - [ ] Queue.cs
  - [ ] QueueEntry.cs
  - [ ] Notification.cs
  - [ ] QueueHistory.cs
  - [ ] ApplicationDbContext.cs

## 📦 Step 2: Restore NuGet Packages

Open terminal in project directory and run:

```bash
dotnet restore
```

**Expected Result**: Should download EF Core packages without errors

- [ ] Packages restored successfully
- [ ] No error messages in terminal

**If errors occur**: Check that HiveQ.csproj includes the Entity Framework packages

## 🗃️ Step 3: Create Initial Migration

Run this command:

```bash
dotnet ef migrations add InitialCreate
```

**Expected Result**: 
- Creates a `Migrations` folder in your project
- Contains files like `20241113_InitialCreate.cs` and `20241113_InitialCreate.Designer.cs`
- Also creates `ApplicationDbContextModelSnapshot.cs`

- [ ] Migrations folder created
- [ ] Migration files generated successfully
- [ ] No error messages

**If "dotnet ef" not found**: Install the tool globally:
```bash
dotnet tool install --global dotnet-ef
```

## 🏗️ Step 4: Create the Database

Run this command:

```bash
dotnet ef database update
```

**Expected Result**:
- Creates `hiveq.db` file in project root
- Console shows "Done" or "Build succeeded"
- No error messages

- [ ] hiveq.db file exists in project root
- [ ] Migration applied successfully
- [ ] No error messages

## 🔍 Step 5: Verify Database

**Option A: Using SQLite Browser**
1. Download DB Browser for SQLite (https://sqlitebrowser.org/)
2. Open hiveq.db file
3. Check that all tables exist:
   - [ ] Users table
   - [ ] Companies table
   - [ ] Queues table
   - [ ] QueueEntries table
   - [ ] Notifications table
   - [ ] QueueHistories table

**Option B: Using VS Code Extension**
1. Install "SQLite" extension in VS Code
2. Right-click hiveq.db → Open Database
3. Explore tables in SQLite Explorer

**Option C: Query in Code**
Add this to a controller action:
```csharp
var company = await _context.Companies.FirstOrDefaultAsync();
return Ok(company?.CompanyName ?? "No data");
```

- [ ] All 6 tables exist
- [ ] Seed data is present (Sample Coffee Shop)

## 🧪 Step 6: Test Database Connection

Create a test action in HomeController:

```csharp
private readonly ApplicationDbContext _context;

public HomeController(ApplicationDbContext context)
{
    _context = context;
}

public async Task<IActionResult> TestDb()
{
    var userCount = await _context.Users.CountAsync();
    var companyCount = await _context.Companies.CountAsync();
    var queueCount = await _context.Queues.CountAsync();
    
    ViewBag.Message = $"Users: {userCount}, Companies: {companyCount}, Queues: {queueCount}";
    return View("Index");
}
```

Run the app and navigate to `/Home/TestDb`

**Expected Result**: Should show "Users: 1, Companies: 1, Queues: 1"

- [ ] Database connection works
- [ ] Can read seed data
- [ ] No errors in browser or console

## 🚀 Step 7: Run Your Application

```bash
dotnet watch
```

- [ ] Application starts successfully
- [ ] No database-related errors in console
- [ ] Can navigate to existing pages

## 🎯 Step 8: Implement First Database Feature

Try implementing one of these features:

### Option A: Display Queues on Homepage
In `HomeController.cs`:
```csharp
public async Task<IActionResult> Index()
{
    var queues = await _context.Queues
        .Include(q => q.Company)
        .Where(q => q.IsActive)
        .ToListAsync();
    
    return View(queues);
}
```

Update `Views/Home/Index.cshtml`:
```html
@model List<HiveQ.Models.Queue>

<h1>Available Queues</h1>
@foreach(var queue in Model)
{
    <div>
        <h3>@queue.QueueName</h3>
        <p>@queue.Company.CompanyName</p>
        <p>Current Size: @queue.CurrentQueueSize / @queue.MaxCapacity</p>
    </div>
}
```

### Option B: Create User Registration Page
Implement the registration logic using the User model

### Option C: Company Dashboard
Show queue statistics for a company

- [ ] Feature implemented successfully
- [ ] Data displays correctly from database
- [ ] No errors when running

## 📚 Step 9: Study the Documentation

Review these files for future development:

- [ ] Read DATABASE_SETUP.md
  - [ ] Understand relationships
  - [ ] Review seed data
  - [ ] Note troubleshooting section

- [ ] Read DATABASE_QUICK_REFERENCE.md
  - [ ] Study CRUD examples
  - [ ] Review complex queries
  - [ ] Understand best practices

- [ ] Read DATABASE_SCHEMA_VISUAL.md
  - [ ] Understand table relationships
  - [ ] Review status field values
  - [ ] Study data flow examples

## 🔐 Step 10: Security Considerations

Before deployment:

- [ ] Replace seed data passwords with proper hashing
- [ ] Implement actual password hashing (use ASP.NET Identity or BCrypt)
- [ ] Add authentication middleware
- [ ] Secure connection string (use User Secrets for development)
- [ ] Validate all user inputs
- [ ] Implement authorization checks

## 🚨 Troubleshooting Checklist

### Issue: "No database provider has been configured"
- [ ] Check Program.cs has AddDbContext configuration
- [ ] Verify using statement: `using HiveQ.Models;`
- [ ] Confirm connection string exists in appsettings.json

### Issue: "Unable to resolve service for type 'ApplicationDbContext'"
- [ ] Verify Program.cs has database service registered
- [ ] Check that `builder.Services.AddDbContext<ApplicationDbContext>` is before `var app = builder.Build();`

### Issue: Migration commands not working
- [ ] Install dotnet-ef tool: `dotnet tool install --global dotnet-ef`
- [ ] Verify EF Core Design package is in .csproj
- [ ] Try `dotnet ef --version` to confirm installation

### Issue: Database file not created
- [ ] Check for errors in migration output
- [ ] Verify connection string path is valid
- [ ] Try absolute path: `Data Source=C:/path/to/hiveq.db`

### Issue: "UNIQUE constraint failed"
- [ ] Check if trying to insert duplicate email, company name, or QR code
- [ ] Review unique constraints in ApplicationDbContext

### Issue: Foreign key constraint errors
- [ ] Ensure parent record exists before creating child
- [ ] Example: User must exist before creating Company
- [ ] Review relationship configuration in ApplicationDbContext

## 📊 Optional: Add Sample Data

Want more test data? Create a database seeder:

```csharp
public static class DatabaseSeeder
{
    public static async Task SeedMoreData(ApplicationDbContext context)
    {
        if (!await context.Users.AnyAsync(u => u.Email == "customer@test.com"))
        {
            var customer = new User
            {
                Email = "customer@test.com",
                PasswordHash = "hashed_password",
                FirstName = "John",
                LastName = "Customer",
                UserType = "Customer",
                PhoneNumber = "5559876543"
            };
            context.Users.Add(customer);
            await context.SaveChangesAsync();
        }
    }
}
```

Call it in Program.cs:
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseSeeder.SeedMoreData(context);
}
```

- [ ] Added additional seed data (optional)

## 🎓 Next Learning Steps

After basic setup works:

- [ ] Learn about Entity Framework migrations
- [ ] Study LINQ queries for data retrieval
- [ ] Implement authentication with ASP.NET Identity
- [ ] Add validation attributes to models
- [ ] Create DTOs (Data Transfer Objects) for API responses
- [ ] Implement repository pattern (optional, for larger projects)
- [ ] Add logging for database operations
- [ ] Write unit tests for database operations

## 🏁 Final Checklist

Before considering database setup complete:

- [ ] Database file exists and contains all tables
- [ ] Seed data is present and accessible
- [ ] Application runs without database errors
- [ ] Can perform basic CRUD operations
- [ ] Reviewed all documentation files
- [ ] Backed up hiveq.db file (for reference)
- [ ] Added .gitignore entry for `*.db` (don't commit database to git)

## 🎉 Success Criteria

You've successfully integrated the database when:

✅ Application starts without errors
✅ Can query and display data from database
✅ Can create new records (add user, queue, etc.)
✅ Can update existing records
✅ All relationships work correctly
✅ Seed data is visible in database browser

---

## 📝 Notes Section

Use this space to track your progress and any issues:

**Date Started**: _______________

**Issues Encountered**:
- 
- 
- 

**Solutions Found**:
- 
- 
- 

**Custom Modifications Made**:
- 
- 
- 

**Next Steps Planned**:
- 
- 
- 

---

**Need Help?** 
- Check DATABASE_SETUP.md for detailed troubleshooting
- Review DATABASE_QUICK_REFERENCE.md for code examples
- Entity Framework Core Docs: https://docs.microsoft.com/en-us/ef/core/

**Questions to Consider**:
- Do you understand the difference between the models and the database?
- Can you explain what a migration is and why it's used?
- Do you know how Entity Framework translates C# objects to SQL?
- Are you comfortable with dependency injection for the DbContext?

Good luck with your HiveQ project! 🚀
