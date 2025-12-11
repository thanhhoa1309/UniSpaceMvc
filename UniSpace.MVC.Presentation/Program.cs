using Microsoft.AspNetCore.DataProtection;
using System.IdentityModel.Tokens.Jwt;
using UniSpace.Model;
using UniSpace.Presentation.Architecture;
using UniSpace.Presentation.Helper;
using UniSpace.Service.BackgroundServices;
using UniSpace.Service.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupIocContainer();
builder.Configuration
 .AddJsonFile("appsettings.json", true, true)
 .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Configure authorization for specific pages
    options.Conventions.AuthorizePage("/Dashboard");
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToFolder("/Auth");
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add Background Services
builder.Services.AddHostedService<BookingCompletionService>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Bind to container network interface; prefer ASPNETCORE_URLS env when present
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:5000";
builder.WebHost.UseUrls(urls);
builder.Services.AddDistributedMemoryCache();

// Configure data protection key persistence
var dataProtectionPath = builder.Configuration["DataProtection:KeyPath"]
 ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_KEY_PATH")
 ?? "/keys";

try
{
    if (!Directory.Exists(dataProtectionPath))
    {
        Directory.CreateDirectory(dataProtectionPath);
    }

    builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("UniSpace");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: could not configure persistent data protection keys at '{dataProtectionPath}': {ex.Message}");
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Apply database migrations and seed data
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    app.ApplyMigrations(app.Logger);

    // Seed data
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<UniSpaceMvcDbContext>();
        await DbSeeder.SeedUsersAsync(dbContext);
        await DbSeeder.SeedCampusesAsync(dbContext);
        await DbSeeder.SeedRoomsAsync(dbContext);
        await DbSeeder.SeedSchedulesAsync(dbContext);
    }
}
catch (Exception e)
{
    app.Logger.LogError(e, "An problem occurred during migration!");
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<BookingHub>("/bookingHub");

app.MapRazorPages();

app.Run();