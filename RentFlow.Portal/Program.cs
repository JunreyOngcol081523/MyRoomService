using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;
using MyRoomService.Infrastructure.Services;

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

// 1. Required for TenantService to access the current user's claims
builder.Services.AddHttpContextAccessor();

// 2. Register the TenantService implementation
// Note: If you haven't moved TenantService to a shared project yet, 
// you may need to add a reference or create a local implementation in the Portal project.
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AdditionalUserClaimsPrincipalFactory>();
builder.Services.AddScoped<ITenantService, TenantService>();
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