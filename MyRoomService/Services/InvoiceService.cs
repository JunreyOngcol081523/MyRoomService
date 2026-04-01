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
                var activeContracts = await _context.Contracts
                    .Include(c => c.Unit)
                    .Where(c => c.TenantId == tenantId && c.Status == ContractStatus.Active)
                    .ToListAsync();

                int generatedCount = 0;
                var readingsToMarkBilled = new HashSet<Guid>();

                foreach (var contract in activeContracts)
                {
                    try
                    {
                        int daysInMonth = DateTime.DaysInMonth(targetDate.Year, targetDate.Month);
                        int actualBillingDay = Math.Min(contract.BillingDay, daysInMonth);
                        var expectedBillingDate = new DateTime(targetDate.Year, targetDate.Month, actualBillingDay);

                        if (targetDate.Date >= expectedBillingDate.Date)
                        {
                            var invoice = await GenerateInvoiceForContractAsync(tenantId, contract.Id, expectedBillingDate, autoPublish, readingsToMarkBilled);

                            if (invoice != null)
                            {
                                generatedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process billing for Contract {ContractId}", contract.Id);
                        continue;
                    }
                }

                if (readingsToMarkBilled.Any())
                {
                    var readings = await _context.MeterReadings
                        .Where(m => readingsToMarkBilled.Contains(m.Id))
                        .ToListAsync();

                    foreach (var reading in readings)
                    {
                        reading.IsBilled = true;
                    }
                    await _context.SaveChangesAsync();
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
                    bool autoPublish = false,
                    HashSet<Guid>? processedReadings = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Occupant)
                    .Include(c => c.AddOns).ThenInclude(a => a.ChargeDefinition)
                    .Include(c => c.Unit).ThenInclude(u => u.UnitServices)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == tenantId);

                if (contract == null || contract.Status != ContractStatus.Active) return null;

                // 🚨 FIX: Duplicate Check now only looks for bills that already have a "RENT" charge.
                // This allows the system to generate a Monthly Bill even if a Move-In Bill exists.
                var regularBillExists = await _context.Invoices.AnyAsync(i =>
                    i.ContractId == contractId
                    && i.InvoiceDate.Month == targetDate.Month
                    && i.InvoiceDate.Year == targetDate.Year
                    && i.Status != "VOID"
                    && i.Items!.Any(item => item.ItemType == "RENT"));

                if (regularBillExists) return null;

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
                        if (service.IsMetered) continue;

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

                // --- SECTION C: METERED UTILITIES (SPLIT LOGIC) ---
                var meteredServiceIds = contract.Unit?.UnitServices.Where(s => s.IsMetered).Select(s => s.Id).ToList();

                if (meteredServiceIds != null && meteredServiceIds.Any())
                {
                    foreach (var serviceId in meteredServiceIds)
                    {
                        var serviceDetail = contract.Unit!.UnitServices.First(s => s.Id == serviceId);

                        var reading = await _context.MeterReadings
                            .Where(m => m.UnitServiceId == serviceId && !m.IsBilled)
                            .OrderByDescending(m => m.ReadingDate)
                            .FirstOrDefaultAsync();

                        if (reading != null)
                        {
                            decimal consumption = (decimal)reading.Consumption;
                            decimal totalAmount = consumption * serviceDetail.MonthlyPrice;

                            decimal finalAmount = totalAmount;
                            string descriptionSuffix = "";

                            if (contract.Unit.MeteredBillingMode == MeteredBillingMode.SplitEqually)
                            {
                                var activeRoommatesCount = await _context.Contracts
                                    .CountAsync(c => c.UnitId == contract.UnitId && c.Status == ContractStatus.Active);

                                if (activeRoommatesCount > 1)
                                {
                                    finalAmount = totalAmount / activeRoommatesCount;
                                    descriptionSuffix = $" (Split 1/{activeRoommatesCount})";
                                }
                            }

                            invoice.Items.Add(new InvoiceItem
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                InvoiceId = invoice.Id,
                                ItemType = "UTILITY",
                                Description = $"{serviceDetail.Name} Usage: {reading.PreviousValue:N2} to {reading.CurrentValue:N2} ({consumption:N2} units){descriptionSuffix}",
                                Amount = finalAmount
                            });
                            runningTotal += finalAmount;

                            if (processedReadings != null)
                            {
                                processedReadings.Add(reading.Id);
                            }
                            else
                            {
                                reading.IsBilled = true;
                            }
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

                // 🚨 --- SECTION E: ADVANCE RENT DEDUCTION LOGIC --- 🚨
                // 1. Find total Advance Rent successfully PAID by this contract
                var totalAdvancePaid = await _context.InvoiceItems
                    .Where(i => i.Invoice!.ContractId == contract.Id
                             && i.ItemType == "ADVANCE"
                             && i.Invoice.Status == "PAID")
                    .SumAsync(i => i.Amount);

                // 2. Find total Advance Rent already APPLIED/CONSUMED in past months
                var totalAdvanceApplied = await _context.InvoiceItems
                    .Where(i => i.Invoice!.ContractId == contract.Id
                             && i.ItemType == "ADVANCE_DEDUCTION")
                    .SumAsync(i => Math.Abs(i.Amount));

                var unusedAdvance = totalAdvancePaid - totalAdvanceApplied;

                if (unusedAdvance > 0)
                {
                    // We only deduct up to the Base Rent amount (so utilities are still paid out of pocket)
                    decimal deductionAmount = Math.Min(unusedAdvance, contract.RentAmount);

                    if (deductionAmount > 0)
                    {
                        invoice.Items.Add(new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            InvoiceId = invoice.Id,
                            ItemType = "ADVANCE_DEDUCTION",
                            Description = "Advance Rent Applied",
                            Amount = -deductionAmount
                        });
                        runningTotal -= deductionAmount;
                    }
                }

                // Finalize total
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