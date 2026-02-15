using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly Guid _tenantId;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
            : base(options)
        {
            _tenantId = tenantService.GetTenantId();
        }
        // Add this inside your ApplicationDbContext class
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        // --- Core Tables ---
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Unit> Units { get; set; }

        // --- NEW: Management & Billing Tables ---
        public DbSet<Occupant> Occupants { get; set; }
        public DbSet<ChargeDefinition> ChargeDefinitions { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractAddOn> ContractAddOns { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configure Decimal Precision (PostgreSQL/SQL Server best practice)
            foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(10, 2)");
            }



            builder.Entity<Building>().Property(b => b.TenantId).HasColumnType("uuid");

            // Keep these to ensure Postgres uses the correct UUID format for all tables
            builder.Entity<Unit>().Property(u => u.TenantId).HasColumnType("uuid");
            builder.Entity<Occupant>().Property(o => o.TenantId).HasColumnType("uuid");
            builder.Entity<Contract>().Property(c => c.TenantId).HasColumnType("uuid");
            builder.Entity<Invoice>().Property(i => i.TenantId).HasColumnType("uuid");
            // 3. Configure Relationships for the New Schema
            builder.Entity<Contract>()
                .HasOne(c => c.Unit)
                .WithMany()
                .HasForeignKey(c => c.UnitId);

            builder.Entity<ContractAddOn>()
                .HasOne(a => a.Contract)
                .WithMany(c => c.AddOns)
                .HasForeignKey(a => a.ContractId);

            // 4. Map Enum to String for BuildingType (matches schema's VARCHAR)
            builder.Entity<Building>()
                .Property(b => b.BuildingType)
                .HasConversion<string>();
            builder.Entity<Occupant>()
                .Property(o => o.KycStatus)
                .HasConversion<string>();
        }
    }
}