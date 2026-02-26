using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(ApplicationDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GenerateMonthlyInvoicesAsync(Guid tenantId, DateTime targetDate, bool autoPublish)
        {
            try
            {
                // Fetch ALL Active contracts instead of strictly matching today's day
                var activeContracts = await _context.Contracts
                    .Where(c => c.TenantId == tenantId && c.Status == ContractStatus.Active)
                    .ToListAsync();

                int generatedCount = 0;

                foreach (var contract in activeContracts)
                {
                    try
                    {
                        // Calculate the correct billing date for this specific month
                        // Math.Min protects against short months (e.g., BillingDay 31 becomes Feb 28)
                        int daysInMonth = DateTime.DaysInMonth(targetDate.Year, targetDate.Month);
                        int actualBillingDay = Math.Min(contract.BillingDay, daysInMonth);

                        var expectedBillingDate = new DateTime(targetDate.Year, targetDate.Month, actualBillingDay);

                        // If today (targetDate) is ON or PAST their billing date for this month, process them!
                        if (targetDate.Date >= expectedBillingDate.Date)
                        {
                            // Pass the 'expectedBillingDate' so the invoice reflects their actual due cycle, 
                            // even if the system is catching up a few days late.
                            var invoice = await GenerateInvoiceForContractAsync(tenantId, contract.Id, expectedBillingDate, autoPublish);

                            if (invoice != null)
                            {
                                generatedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process billing for Contract {ContractId}", contract.Id);
                        continue; // Keep going if one contract fails
                    }
                }

                return generatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure running the billing cycle.");
                throw;
            }
        }

        public async Task<Invoice?> GenerateInvoiceForContractAsync(
            Guid tenantId,
            Guid contractId,
            DateTime targetDate,
            bool autoPublish = false)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Load Contract + Unit + ALL Services + AddOns
                var contract = await _context.Contracts
                    .Include(c => c.Occupant)
                    .Include(c => c.AddOns).ThenInclude(a => a.ChargeDefinition)
                    .Include(c => c.Unit).ThenInclude(u => u.UnitServices)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == tenantId);

                if (contract == null || contract.Status != ContractStatus.Active) return null;

                // 2. Duplicate Check (Rule 1 & Rule 5 safety)
                var exists = await _context.Invoices.AnyAsync(i => i.ContractId == contractId
                    && i.InvoiceDate.Month == targetDate.Month && i.InvoiceDate.Year == targetDate.Year && i.Status != "VOID");
                if (exists) return null;

                // 3. Initialize Invoice
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ContractId = contract.Id,
                    OccupantId = contract.OccupantId,
                    InvoiceDate = targetDate,
                    DueDate = targetDate.AddDays(5),
                    Status = "UNPAID",
                    IsPublished = autoPublish,
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<InvoiceItem>()
                };

                decimal runningTotal = 0;

                // --- SECTION A: BASE RENT ---
                var rentItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvoiceId = invoice.Id,
                    ItemType = "RENT",
                    Description = $"Base Rent - {targetDate:MMM yyyy}",
                    Amount = contract.RentAmount
                };
                invoice.Items.Add(rentItem);
                runningTotal += rentItem.Amount;

                // --- SECTION B: UNIT SERVICES (ONLY FIXED SERVICES) ---
                if (contract.Unit?.UnitServices != null)
                {
                    foreach (var service in contract.Unit.UnitServices)
                    {
                        // CRITICAL: We skip metered services here because they are handled in Section C
                        if (service.IsMetered) continue;

                        // Only add Fixed Monthly Services (WiFi, Trash, Parking, etc.)
                        invoice.Items.Add(new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            InvoiceId = invoice.Id,
                            ItemType = "SERVICE",
                            Description = service.Name,
                            Amount = service.MonthlyPrice
                        });
                        runningTotal += service.MonthlyPrice;
                    }
                }

                // --- SECTION C: METERED UTILITIES (CONSUMPTION-BASED) ---
                // We look for any metered service assigned to this unit
                var meteredServiceIds = contract.Unit?.UnitServices
                    .Where(s => s.IsMetered)
                    .Select(s => s.Id).ToList();

                if (meteredServiceIds != null && meteredServiceIds.Any())
                {
                    foreach (var serviceId in meteredServiceIds)
                    {
                        var serviceDetail = contract.Unit!.UnitServices.First(s => s.Id == serviceId);

                        // Find the latest UNBILLED reading
                        var reading = await _context.MeterReadings
                            .Where(m => m.UnitServiceId == serviceId && !m.IsBilled)
                            .OrderByDescending(m => m.ReadingDate)
                            .FirstOrDefaultAsync();

                        if (reading != null)
                        {
                            decimal consumption = (decimal)reading.Consumption;
                            decimal amount = consumption * serviceDetail.MonthlyPrice; // Price acts as the Rate/kWh

                            invoice.Items.Add(new InvoiceItem
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                InvoiceId = invoice.Id,
                                ItemType = "UTILITY",
                                Description = $"{serviceDetail.Name} Usage: {reading.PreviousValue:N2} to {reading.CurrentValue:N2} ({consumption:N2} units)",
                                Amount = amount
                            });
                            runningTotal += amount;

                            // Mark as billed so it isn't picked up by next cycle
                            reading.IsBilled = true;
                        }
                    }
                }

                // --- SECTION D: CONTRACT ADD-ONS ---
                if (contract.AddOns != null)
                {
                    foreach (var addon in contract.AddOns)
                    {
                        bool isRecurring = string.Equals(addon.ChargeDefinition?.ChargeType, "RECURRING", StringComparison.OrdinalIgnoreCase);
                        bool isOneTime = string.Equals(addon.ChargeDefinition?.ChargeType, "ONE_TIME", StringComparison.OrdinalIgnoreCase);

                        if (isRecurring || (isOneTime && !addon.IsProcessed))
                        {
                            invoice.Items.Add(new InvoiceItem
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                InvoiceId = invoice.Id,
                                ItemType = "ADDON",
                                Description = addon.ChargeDefinition!.Name,
                                Amount = addon.AgreedAmount
                            });
                            runningTotal += addon.AgreedAmount;
                            if (isOneTime) addon.IsProcessed = true;
                        }
                    }
                }

                invoice.TotalAmount = runningTotal;
                _context.Invoices.Add(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate invoice for contract {Id}", contractId);
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}