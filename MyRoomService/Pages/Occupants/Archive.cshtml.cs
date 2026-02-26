using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Occupants
{
    public class ArchiveModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ArchiveModel> _logger;

        public ArchiveModel(ApplicationDbContext context, ITenantService tenantService, ILogger<ArchiveModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        [BindProperty]
        public Occupant Occupant { get; set; } = default!;

        public bool CanArchive { get; set; }
        public string ArchiveMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Occupants", "/Occupants"),
                ("Archive", "")
            };
            if (id == null) return NotFound();

            var tenantId = _tenantService.GetTenantId();

            var occupant = await _context.Occupants
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId);

            if (occupant == null) return NotFound();

            Occupant = occupant;
            await ValidateArchiveEligibilityAsync(tenantId, occupant);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantService.GetTenantId();
            var occupant = await _context.Occupants
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

            if (occupant == null) return NotFound();

            await ValidateArchiveEligibilityAsync(tenantId, occupant);

            if (!CanArchive)
            {
                TempData["ErrorMessage"] = "Validation failed: " + ArchiveMessage;
                return Page();
            }

            try
            {
                occupant.IsArchived = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Occupant {OccupantId} successfully archived.", id);
                TempData["StatusMessage"] = "Occupant has been successfully archived.";

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive occupant {OccupantId}", id);
                TempData["ErrorMessage"] = "A critical error occurred while archiving the occupant.";
                return Page();
            }
        }

        private async Task ValidateArchiveEligibilityAsync(Guid tenantId, Occupant occupant)
        {
            CanArchive = true;
            ArchiveMessage = "This occupant meets all requirements and can be safely archived.";

            // Rule 1: No Unpaid Invoices
            var hasUnpaidInvoices = await _context.Invoices
                .AnyAsync(i => i.OccupantId == occupant.Id && i.TenantId == tenantId && i.Status == "UNPAID");

            if (hasUnpaidInvoices)
            {
                CanArchive = false;
                ArchiveMessage = "Cannot archive: This occupant still has UNPAID invoices.";
                return;
            }

            // Rule 2 & 3: Contract Rules
            if (occupant.Contracts != null && occupant.Contracts.Any())
            {
                var activeStatuses = new[] { ContractStatus.Active,  ContractStatus.Reserved };

                if (occupant.Contracts.Any(c => activeStatuses.Contains(c.Status)))
                {
                    CanArchive = false;
                    ArchiveMessage = "Cannot archive: Occupant currently has an Active, Pending, or Reserved contract.";
                    return;
                }

                // Find the most recent contract end date. If null, treat as today so the 1-month wait is enforced.
                var latestEndDate = occupant.Contracts.Max(c => c.EndDate ?? DateTime.UtcNow);

                // Ensure 1 full month has passed since the contract ended/terminated
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

                if (latestEndDate > oneMonthAgo)
                {
                    CanArchive = false;
                    ArchiveMessage = $"Cannot archive: The latest contract ended on {latestEndDate:MMM dd, yyyy}. You must wait 1 full month after the contract ends before archiving.";
                    return;
                }
            }
            // If they have 0 contracts and 0 unpaid invoices, they are just an empty lead and can be archived.
        }
    }
}