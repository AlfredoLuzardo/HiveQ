using DotNetEnv;
using HiveQ.Models;
using HiveQ.Services;
using HiveQ.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

Env.Load(); // Load environment variables from .env file

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add SignalR
builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

builder.Services.AddScoped<
    HiveQ.Services.IWaitTimePredictionService,
    HiveQ.Services.WaitTimePredictionService
>();

// Register custom services
builder.Services.AddScoped<AuthenticationService>();

// Only register SMS service if Twilio is configured
var twilioAccountSid = builder.Configuration["Twilio:AccountSid"];
if (!string.IsNullOrEmpty(twilioAccountSid))
{
    builder.Services.AddScoped<ISmsService, SmsService>();
    Console.WriteLine("✓ SMS Service configured with Twilio");
}
else
{
    Console.WriteLine("⚠ SMS Service not configured - Twilio credentials missing");
}

// Configure SQLite Database with connection pooling and WAL mode
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    // Use SQLite locally for development
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString)
    );
}
else
{
    // Use SQL Server in Production (Azure)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString)
    );
}

var app = builder.Build();

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // This will apply all pending migrations including AddProfilePictureToUser
        dbContext.Database.Migrate();
        Console.WriteLine("✓ Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration warning: {ex.Message}");
        // Try to manually fix the migration history
        try
        {
            dbContext.Database.ExecuteSqlRaw(
                "INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251117034405_InitialCreate', '9.0.0')"
            );
            dbContext.Database.Migrate();
            Console.WriteLine("✓ Database fixed and migrations applied");
        }
        catch
        {
            Console.WriteLine(
                "Note: If you see column errors, the migration may have already been applied"
            );
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Map SignalR hub
app.MapHub<QueueHub>("/queueHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
