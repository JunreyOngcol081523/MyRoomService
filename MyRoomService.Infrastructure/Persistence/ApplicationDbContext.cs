using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Added this
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
namespace MyRoomService.Infrastructure.Persistence
{
    // We change "DbContext" to "IdentityDbContext<ApplicationUser>"
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ITenantService _tenantService;

        // We pass the TenantId into the Context directly
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
            ITenantService tenantService)
            : base(options)
        {
            _tenantService = tenantService;

        }

        public DbSet<Building> Buildings { get; set; }
        // Add this line inside your ApplicationDbContext class
        public DbSet<Tenant> Tenants { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // PRODUCTION RULE: Automatically filter EVERY query for Buildings
            // This makes it impossible to accidentally see another tenant's data.
            builder.Entity<Building>().HasQueryFilter(b => b.TenantId == _tenantService.GetTenantId());
        }

    }
}