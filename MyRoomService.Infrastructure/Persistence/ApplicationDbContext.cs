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
        public DbSet<MeterReading> MeterReadings { get; set; }
        public DbSet<ContractIncludedService> ContractIncludedServices { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configure Decimal Precision (Globally for all decimals)
            foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(10, 2)");
            }

            // 2. Ensure Postgres UUID types
            var uuidEntities = new[] { "Building", "Unit", "Occupant", "Contract", "Invoice", "MeterReading", "InvoiceItem", "ContractIncludedService" };
            foreach (var entityName in uuidEntities)
            {
                var entity = builder.Model.FindEntityType($"MyRoomService.Domain.Entities.{entityName}");
                if (entity != null)
                {
                    var tenantProp = entity.FindProperty("TenantId");
                    if (tenantProp != null) tenantProp.SetColumnType("uuid");
                }
            }

            // 3. Contract Configuration
            builder.Entity<Contract>(entity =>
            {
                entity.Ignore("ContractId"); // Ignore any shadow property issues

                entity.HasOne(c => c.Unit)
                    .WithMany(u => u.Contracts)
                    .HasForeignKey(c => c.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Occupant)
                    .WithMany(o => o.Contracts)
                    .HasForeignKey(c => c.OccupantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(c => c.Status)
                    .HasConversion<int>();
            });

            // 4. ContractAddOn Configuration
            builder.Entity<ContractAddOn>(entity =>
            {
                entity.HasOne(a => a.Contract)
                    .WithMany(c => c.AddOns)
                    .HasForeignKey(a => a.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.ChargeDefinition)
                    .WithMany()
                    .HasForeignKey(a => a.ChargeDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 5. ContractIncludedService Configuration
            // ContractIncludedService Configuration
            builder.Entity<ContractIncludedService>(entity =>
            {
                entity.HasOne(cis => cis.Contract)
                    .WithMany(c => c.IncludedServices)
                    .HasForeignKey(cis => cis.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cis => cis.UnitService)
                    .WithMany()
                    .HasForeignKey(cis => cis.UnitServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 6. Invoice Configuration
            builder.Entity<Invoice>(entity =>
            {
                entity.HasOne(i => i.Contract)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(i => i.ContractId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Occupant)
                    .WithMany()
                    .HasForeignKey(i => i.OccupantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 7. InvoiceItem Configuration
            builder.Entity<InvoiceItem>(entity =>
            {
                // Link Item to Parent Invoice
                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.Items)
                    .HasForeignKey(d => d.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Link Item to Contract AddOn (Optional Source)
                entity.HasOne(d => d.ContractAddOn)
                    .WithMany()
                    .HasForeignKey(d => d.ContractAddOnId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                // Link Item to Unit Service (Optional Source)
                entity.HasOne(d => d.UnitService)
                    .WithMany()
                    .HasForeignKey(d => d.UnitServiceId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // 8. MeterReading Configuration
            builder.Entity<MeterReading>(entity =>
            {
                entity.HasOne(m => m.UnitService)
                    .WithMany()
                    .HasForeignKey(m => m.UnitServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 9. Other Entity Configurations
            builder.Entity<Building>()
                .Property(b => b.BuildingType)
                .HasConversion<string>();

            builder.Entity<Occupant>()
                .Property(o => o.KycStatus)
                .HasConversion<string>();

            // 10. Seed Default Roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1a1c3b5d-8a5f-4a3b-9c2d-1e1f2a3b4c5d",
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