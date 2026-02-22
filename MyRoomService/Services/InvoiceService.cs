using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> GenerateMonthlyInvoicesAsync(Guid tenantId, DateTime targetDate)
        {
            // Find all active contracts meant to be billed on this day
            var eligibleContracts = await _context.Contracts
                .Where(c => c.TenantId == tenantId
                         && c.Status == ContractStatus.Active
                         && c.BillingDay == targetDate.Day)
                .ToListAsync();

            int generatedCount = 0;

            foreach (var contract in eligibleContracts)
            {
                var invoice = await GenerateInvoiceForContractAsync(tenantId, contract.Id, targetDate);
                if (invoice != null)
                {
                    generatedCount++;
                }
            }

            return generatedCount;
        }

        public async Task<Invoice?> GenerateInvoiceForContractAsync(Guid tenantId, Guid contractId, DateTime targetDate)
        {
            // 1. Fetch Contract with all its financial attachments
            var contract = await _context.Contracts
                .Include(c => c.AddOns)
                    .ThenInclude(a => a.ChargeDefinition)
                // .Include(c => c.IncludedServices) // Uncomment when you add the snapshot table!
                .FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == tenantId);

            if (contract == null || contract.Status != ContractStatus.Active)
                return null;

            // 2. Prevent duplicate invoices for the same month/year
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.ContractId == contractId
                                       && i.InvoiceDate.Month == targetDate.Month
                                       && i.InvoiceDate.Year == targetDate.Year);

            if (existingInvoice != null)
                return null; // Already billed this month!

            // 3. Initialize the new Invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ContractId = contract.Id,
                OccupantId = contract.OccupantId,
                InvoiceDate = targetDate,
                DueDate = targetDate.AddDays(5), // E.g., Due 5 days after billing
                Status = "UNPAID",
                CreatedAt = DateTime.UtcNow,
                Items = new List<InvoiceItem>()
            };

            decimal runningTotal = 0;

            // --- ADD ITEM 1: Base Rent ---
            var rentItem = new InvoiceItem
            {
                TenantId = tenantId,
                ItemType = "RENT",
                Description = $"Base Rent for {targetDate:MMM yyyy}",
                Amount = contract.RentAmount
            };
            invoice.Items.Add(rentItem);
            runningTotal += rentItem.Amount;

            // --- ADD ITEM 2: Contract Add-Ons ---
            foreach (var addon in contract.AddOns)
            {
                var addonItem = new InvoiceItem
                {
                    TenantId = tenantId,
                    ItemType = "ADD_ON",
                    Description = addon.ChargeDefinition?.Name ?? "Additional Charge",
                    Amount = addon.AgreedAmount
                };
                invoice.Items.Add(addonItem);
                runningTotal += addonItem.Amount;
            }

            // --- ADD ITEM 3: Snapshotted Unit Services (When ready) ---

            foreach (var unitService in contract.IncludedServices)
            {
                var serviceItem = new InvoiceItem
                {
                    TenantId = tenantId,
                    ItemType = "UNIT_SERVICE",
                    Description = unitService.Name,
                    Amount = unitService.Amount
                };
                invoice.Items.Add(serviceItem);
                runningTotal += serviceItem.Amount;
            }


            // 4. Finalize Total and Save
            invoice.TotalAmount = runningTotal;
            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            return invoice;
        }
    }
}