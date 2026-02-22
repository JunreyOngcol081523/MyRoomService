using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace MyRoomService.Pages.Invoices
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public DetailsModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public Invoice Invoice { get; set; } = default!;

        // 1. We bind this property to catch the form submission from the Modal
        [BindProperty]
        public AdjustmentInputModel Adjustment { get; set; } = new();

        public class AdjustmentInputModel
        {
            [Required]
            public Guid InvoiceId { get; set; }
            [Required]
            public string Type { get; set; } = string.Empty; // "Reward" or "Penalty"
            [Required]
            public string Description { get; set; } = string.Empty;
            [Required]
            [Range(0.01, 100000, ErrorMessage = "Amount must be greater than zero.")]
            public decimal Amount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantService.GetTenantId();

            var invoice = await _context.Invoices
                .Include(i => i.Occupant)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Unit)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId);

            if (invoice == null) return NotFound();

            Invoice = invoice;
            return Page();
        }

        // 2. The POST handler for the Modal
        public async Task<IActionResult> OnPostAddAdjustmentAsync()
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage(new { id = Adjustment.InvoiceId });
            }

            var tenantId = _tenantService.GetTenantId();

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == Adjustment.InvoiceId && i.TenantId == tenantId);

            if (invoice == null) return NotFound();

            // Smart Logic: Convert Reward to a negative number
            decimal finalAmount = Adjustment.Type == "Reward"
                ? -Math.Abs(Adjustment.Amount)
                : Math.Abs(Adjustment.Amount);

            string itemType = Adjustment.Type == "Reward" ? "DISCOUNT" : "PENALTY";

            // 1. Create the new line item WITHOUT manually setting the Id
            var newItem = new InvoiceItem
            {
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                ItemType = itemType,
                Description = Adjustment.Description,
                Amount = finalAmount
            };

            // 2. EXPLICITLY tell EF Core to track this as a brand NEW record (INSERT)
            _context.Add(newItem);

            // 3. Update the invoice total amount directly
            invoice.TotalAmount += finalAmount;

            // 4. Save to database
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = invoice.Id });
        }
        // Add this property to catch the input from the modal
        [BindProperty]
        public decimal PaymentAmount { get; set; }

        // Add this new POST handler
        public async Task<IActionResult> OnPostRecordPaymentAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

            if (invoice == null) return NotFound();

            if (PaymentAmount <= 0)
            {
                TempData["StatusMessage"] = "Error: Payment amount must be greater than zero.";
                return RedirectToPage(new { id = id });
            }

            // Add the new payment to the running total
            invoice.AmountPaid += PaymentAmount;

            // Check if the invoice is now fully paid
            if (invoice.AmountPaid >= invoice.TotalAmount)
            {
                invoice.Status = "PAID";
                invoice.AmountPaid = invoice.TotalAmount; // Cap it so we don't show negative balances
                TempData["StatusMessage"] = "Success! Invoice is fully paid.";

                await _context.SaveChangesAsync();

                // Redirect back to the active invoices list since this one is done!
                return RedirectToPage("./Index");
            }
            else
            {
                // It's a partial payment
                invoice.Status = "PARTIAL";
                TempData["StatusMessage"] = $"Recorded partial payment of ₱{PaymentAmount:N2}. Remaining balance: ₱{(invoice.TotalAmount - invoice.AmountPaid):N2}";

                await _context.SaveChangesAsync();

                // Stay on the details page so they can see the updated balance
                return RedirectToPage(new { id = id });
            }
        }
    }
}