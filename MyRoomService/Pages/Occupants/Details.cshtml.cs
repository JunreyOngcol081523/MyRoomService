using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Occupants
{
    public class DetailsModel : PageModel
    {
        private readonly MyRoomService.Infrastructure.Persistence.ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(MyRoomService.Infrastructure.Persistence.ApplicationDbContext context, ITenantService tenantService, ILogger<DetailsModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        public Occupant Occupant { get; set; } = default!;
        public decimal TotalUnpaidBalance { get; set; }

        // Rule 5: Collections must be limited/paginated
        public List<Contract> Contracts { get; set; } = new();
        public List<Invoice> RecentUnpaidInvoices { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 5;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // Rule 2: Breadcrumbs safely at the top
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Occupants", "/Occupants"),
                ("Occupant Profile", "")
            };

            if (id == null) return NotFound();

            // Rule 1: Every method wrapped in try-catch
            try
            {
                var tenantId = _tenantService.GetTenantId();

                var occupant = await _context.Occupants
                    .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

                if (occupant == null) return NotFound();

                Occupant = occupant;

                // Update breadcrumb with actual name once loaded
                ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
                {
                    ("Occupants", "/Occupants"),
                    (Occupant.FullName, $"")
                };

                // Feature: Financial Health Snapshot
                TotalUnpaidBalance = await _context.Invoices
                    .Where(i => i.OccupantId == id && i.TenantId == tenantId && i.Status == "UNPAID")
                    .SumAsync(i => i.TotalAmount);

                // Feature: Top 5 Pending Invoices (Rule 5 limit)
                RecentUnpaidInvoices = await _context.Invoices
                    .Where(i => i.OccupantId == id && i.TenantId == tenantId && i.Status == "UNPAID")
                    .OrderBy(i => i.DueDate)
                    .Take(5)
                    .ToListAsync();

                // Rule 5: Paginated Contracts Query
                var contractsQuery = _context.Contracts
                    .Include(c => c.Unit)
                        .ThenInclude(u => u.Building)
                    .Where(c => c.OccupantId == id && c.TenantId == tenantId)
                    .OrderByDescending(c => c.StartDate);

                int totalContracts = await contractsQuery.CountAsync();
                TotalPages = (int)Math.Ceiling(totalContracts / (double)PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (TotalPages > 0 && CurrentPage > TotalPages) CurrentPage = TotalPages;

                Contracts = await contractsQuery
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading occupant details for ID: {Id}", id);
                TempData["ErrorMessage"] = "A critical error occurred while loading the occupant profile.";
                return RedirectToPage("./Index");
            }
        }

        // HANDLER: Verify
        public async Task<IActionResult> OnPostVerifyAsync(Guid id)
        {
            // Rule 1: Safe Try-Catch block
            try
            {
                var tenantId = _tenantService.GetTenantId();
                var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

                if (occupant != null)
                {
                    occupant.KycStatus = KycStatus.Verified;
                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Occupant KYC status verified successfully.";
                }

                return RedirectToPage(new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying KYC for occupant {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while trying to verify the occupant.";
                return RedirectToPage(new { id = id });
            }
        }

        // HANDLER: Reject
        public async Task<IActionResult> OnPostRejectAsync(Guid id)
        {
            // Rule 1: Safe Try-Catch block
            try
            {
                var tenantId = _tenantService.GetTenantId();
                var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

                if (occupant != null)
                {
                    occupant.KycStatus = KycStatus.Rejected;
                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Occupant KYC status rejected.";
                }

                return RedirectToPage(new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting KYC for occupant {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while trying to reject the occupant.";
                return RedirectToPage(new { id = id });
            }
        }
    }
}