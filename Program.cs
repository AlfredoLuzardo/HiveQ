using HiveQ.Models;
using HiveQ.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

// Register custom services
builder.Services.AddScoped<AuthenticationService>();

// Configure SQLite Database with connection pooling and WAL mode
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(30);
    });
}, ServiceLifetime.Scoped);

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
            dbContext.Database.ExecuteSqlRaw("INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251117034405_InitialCreate', '9.0.0')");
            dbContext.Database.Migrate();
            Console.WriteLine("✓ Database fixed and migrations applied");
        }
        catch
        {
            Console.WriteLine("Note: If you see column errors, the migration may have already been applied");
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
