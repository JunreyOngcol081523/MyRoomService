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
                var eligibleContracts = await _context.Contracts
                    .Where(c => c.TenantId == tenantId
                             && c.Status == ContractStatus.Active
                             && c.BillingDay == targetDate.Day)
                    .ToListAsync();

                int generatedCount = 0;
                foreach (var contract in eligibleContracts)
                {
                    try
                    {
                        var invoice = await GenerateInvoiceForContractAsync(tenantId, contract.Id, targetDate, autoPublish);
                        if (invoice != null) generatedCount++;
                    }
                    catch (Exception)
                    {
                        continue; // Keep going if one contract fails
                    }
                }
                return generatedCount;
            }
            catch (Exception) { throw; }
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
                _logger.LogInformation("========== INVOICE GENERATION START ==========");
                _logger.LogInformation("Contract ID: {ContractId}", contractId);
                _logger.LogInformation("Target Date: {TargetDate:yyyy-MM-dd}", targetDate);

                // Load contract with explicit includes and AsSplitQuery to handle large data sets safely
                var contract = await _context.Contracts
                    .Include(c => c.AddOns)
                        .ThenInclude(a => a.ChargeDefinition)
                    .Include(c => c.IncludedServices)
                        .ThenInclude(cis => cis.UnitService)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == tenantId);

                if (contract == null)
                {
                    _logger.LogWarning("❌ Contract not found for ID: {ContractId}", contractId);
                    return null;
                }

                _logger.LogInformation("✅ Contract loaded: Status={Status}, Rent={Rent:C}", contract.Status, contract.RentAmount);
                _logger.LogInformation("AddOns collection: {Count}", contract.AddOns?.Count.ToString() ?? "NULL");
                _logger.LogInformation("IncludedServices collection: {Count}", contract.IncludedServices?.Count.ToString() ?? "NULL");

                if (contract.Status != ContractStatus.Active)
                {
                    _logger.LogWarning("❌ Contract not active. Current Status: {Status}", contract.Status);
                    return null;
                }

                // Duplicate check for the billing period
                var existingInvoice = await _context.Invoices
                    .AnyAsync(i => i.ContractId == contractId
                                && i.InvoiceDate.Month == targetDate.Month
                                && i.InvoiceDate.Year == targetDate.Year
                                && i.Status != "VOID");

                if (existingInvoice)
                {
                    _logger.LogWarning("❌ Invoice already exists for this period ({Month}/{Year})", targetDate.Month, targetDate.Year);
                    return null;
                }

                // Initialize invoice
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ContractId = contract.Id,
                    OccupantId = contract.OccupantId,
                    InvoiceDate = targetDate,
                    DueDate = targetDate.AddDays(5),
                    Status = "UNPAID",
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = autoPublish,
                    Items = new List<InvoiceItem>()
                };

                decimal runningTotal = 0;
                int itemCount = 0;

                // --- BASE RENT ---
                _logger.LogInformation("--- Adding Rent ---");
                var rentItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvoiceId = invoice.Id,
                    ItemType = "RENT",
                    Description = $"Base Rent for {targetDate:MMM yyyy}",
                    Amount = contract.RentAmount
                };
                invoice.Items.Add(rentItem);
                runningTotal += rentItem.Amount;
                itemCount++;
                _logger.LogInformation("✅ Added: {Description} - {Amount:C}", rentItem.Description, rentItem.Amount);

                // --- ADD-ONS ---
                // --- ADD-ONS ---
                _logger.LogInformation("--- Processing AddOns ---");

                if (contract.AddOns != null && contract.AddOns.Any())
                {
                    foreach (var addon in contract.AddOns)
                    {
                        var charge = addon.ChargeDefinition;
                        if (charge == null) continue;

                        // Use .Trim() and OrdinalIgnoreCase to handle spaces and casing
                        string type = charge.ChargeType?.Trim() ?? "";
                        bool isRecurring = string.Equals(type, "RECURRING", StringComparison.OrdinalIgnoreCase);
                        bool isOneTime = string.Equals(type, "ONE_TIME", StringComparison.OrdinalIgnoreCase);

                        // Final logic check
                        bool shouldInclude = isRecurring || (isOneTime && !addon.IsProcessed);

                        _logger.LogInformation("  Checking AddOn: {Name} | Type: '{Type}' | IsProcessed: {Proc} | ShouldInclude: {Should}",
                            charge.Name, type, addon.IsProcessed, shouldInclude);

                        if (shouldInclude)
                        {
                            var addonItem = new InvoiceItem
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                InvoiceId = invoice.Id,
                                ItemType = "ADD_ON",
                                Description = charge.Name,
                                Amount = addon.AgreedAmount,
                                ContractAddOnId = addon.Id
                            };

                            invoice.Items.Add(addonItem);
                            runningTotal += addonItem.Amount;
                            itemCount++;

                            if (isOneTime)
                            {
                                addon.IsProcessed = true;
                                _logger.LogInformation("  -> Marked {Name} as Processed.", charge.Name);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No AddOns found for this contract in the database.");
                }

                // --- INCLUDED SERVICES ---
                _logger.LogInformation("--- Processing IncludedServices ---");
                if (contract.IncludedServices != null && contract.IncludedServices.Any())
                {
                    foreach (var contractService in contract.IncludedServices)
                    {
                        var unitService = contractService.UnitService;
                        if (unitService == null)
                        {
                            _logger.LogWarning("Service {ServiceId} has null UnitService. Skipping.", contractService.Id);
                            continue;
                        }

                        var amount = contractService.OverrideAmount ?? unitService.MonthlyPrice;
                        var serviceItem = new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            InvoiceId = invoice.Id,
                            ItemType = "UNIT_SERVICE",
                            Description = unitService.Name,
                            Amount = amount,
                            UnitServiceId = unitService.Id
                        };

                        invoice.Items.Add(serviceItem);
                        runningTotal += serviceItem.Amount;
                        itemCount++;
                        _logger.LogInformation("✅ Added Service: {Name} - {Amount:C}", unitService.Name, amount);
                    }
                }

                // Finalize
                invoice.TotalAmount = runningTotal;

                _logger.LogInformation("--- Summary --- Items Created: {ItemCount}, Total: {Total:C}", itemCount, invoice.TotalAmount);

                // Save
                _logger.LogInformation("--- Saving to Database ---");
                _context.Invoices.Add(invoice);

                var savedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync: {Count} rows affected", savedCount);

                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully");

                // Verification check
                var savedInvoice = await _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                if (savedInvoice != null)
                {
                    _logger.LogInformation("✅ Verification: Invoice verified in DB with {ItemCount} items.", savedInvoice.Items.Count);
                }
                else
                {
                    _logger.LogError("❌ Verification Failed: Invoice not found in DB after commit!");
                }

                _logger.LogInformation("========== GENERATION COMPLETE ==========");
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EXCEPTION during invoice generation for Contract {ContractId}", contractId);
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}