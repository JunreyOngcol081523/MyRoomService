using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Infrastructure.Persistence;

// 1. PostgreSQL Timestamp Compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 2. Database Configuration (Pointing to your Shared PostgreSQL DB)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Identity Configuration (Using ApplicationUser)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// 4. Portal Authorization Policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOccupant", policy => policy.RequireRole("Occupant"));
});

// 5. Global Folder Restrictions
builder.Services.AddRazorPages(options =>
{
    // Force the entire website to require the "Occupant" role
    options.Conventions.AuthorizeFolder("/", "RequireOccupant");

    // Allow anyone to access the Login/Logout/Register pages
    options.Conventions.AllowAnonymousToAreaFolder("Identity", "/Account");
});

var app = builder.Build();

// 6. HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Note: Changed from MapStaticAssets for standard Razor Pages setup

app.UseRouting();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();