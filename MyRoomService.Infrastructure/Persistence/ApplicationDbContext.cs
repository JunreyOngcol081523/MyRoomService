using Microsoft.AspNetCore.Identity;
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

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitService> UnitServices { get; set; }
        public DbSet<Occupant> Occupants { get; set; }
        public DbSet<ChargeDefinition> ChargeDefinitions { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractAddOn> ContractAddOns { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configure Decimal Precision
            foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(10, 2)");
            }

            // 2. Ensure Postgres UUID types
            builder.Entity<Building>().Property(b => b.TenantId).HasColumnType("uuid");
            builder.Entity<Unit>().Property(u => u.TenantId).HasColumnType("uuid");
            builder.Entity<Occupant>().Property(o => o.TenantId).HasColumnType("uuid");
            builder.Entity<Contract>().Property(c => c.TenantId).HasColumnType("uuid");
            builder.Entity<Invoice>().Property(i => i.TenantId).HasColumnType("uuid");

            // 3. FIX: Specific Relationship Mapping

            // Standard relationships from your ERD
            builder.Entity<Contract>().Ignore("ContractId");
            builder.Entity<Contract>()
                .HasOne(c => c.Unit)
                .WithMany(u => u.Contracts)
                .HasForeignKey(c => c.UnitId);

            builder.Entity<Contract>()
                .HasOne(c => c.Occupant)
                .WithMany(o => o.Contracts)
                .HasForeignKey(c => c.OccupantId);

            // Link Contract to AddOns (Based on ERD)
            builder.Entity<ContractAddOn>()
                .HasOne(a => a.Contract)
                .WithMany(c => c.AddOns)
                .HasForeignKey(a => a.ContractId);
            // Link Contract to Invoices (Based on ERD)
            builder.Entity<Invoice>()
                .HasOne(i => i.Contract)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.ContractId);

            // 4. Enum Conversions
            builder.Entity<Building>().Property(b => b.BuildingType).HasConversion<string>();
            builder.Entity<Occupant>().Property(o => o.KycStatus).HasConversion<string>();

            // Ensure Contract Status maps to Integer (matches your screenshot)
            builder.Entity<Contract>()
                .Property(c => c.Status)
                .HasConversion<int>();
            // Seed Default Roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1a1c3b5d-8a5f-4a3b-9c2d-1e1f2a3b4c5d", // Use a static Guid for seeded data
                    Name = "SystemAdmin",
                    NormalizedName = "SYSTEMADMIN",
                    ConcurrencyStamp = "1a1c3b5d-8a5f-4a3b-9c2d-1e1f2a3b4c5d"
                },
                new IdentityRole
                {
                    Id = "2b2d4c6e-9b6g-5b4c-0d3e-2f2g3b4c5d6e",
                    Name = "Landlord",
                    NormalizedName = "LANDLORD",
                    ConcurrencyStamp = "2b2d4c6e-9b6g-5b4c-0d3e-2f2g3b4c5d6e"
                },
                new IdentityRole
                {
                    Id = "3c3e5d7f-0c7h-6c5d-1e4f-3g3h4c5d6e7f",
                    Name = "Occupant",
                    NormalizedName = "OCCUPANT",
                    ConcurrencyStamp = "3c3e5d7f-0c7h-6c5d-1e4f-3g3h4c5d6e7f"
                }
            );
        }
    }
}