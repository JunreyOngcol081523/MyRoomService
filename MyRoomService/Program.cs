using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;
using MyRoomService.Services;
using MyRoomService.Web.Services; // Ensure this points to your actual context folder

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// 1. Get the Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Setup PostgreSQL (Remove the SQL Server block!)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// Required to allow the service to "see" the browser's context
builder.Services.AddHttpContextAccessor();

// Register our custom Tenant Service
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// 3. Setup Identity to use ApplicationUser (CLEAN & MERGED)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AdditionalUserClaimsPrincipalFactory>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Note: Changed MapStaticAssets back to UseStaticFiles for compatibility

app.UseRouting();

app.UseAuthentication(); // IMPORTANT: You were missing this line!
app.UseAuthorization();

app.MapRazorPages();

app.Run();