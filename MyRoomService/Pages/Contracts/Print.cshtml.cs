using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Contracts
{
    public class PrintModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<PrintModel> _logger;

        public PrintModel(ApplicationDbContext context, ITenantService tenantService, ILogger<PrintModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        public Contract Contract { get; set; } = default!;
        public string OwnerName { get; set; } = "Property Management";

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            // Rule 2: Every OnGet has breadcrumbs (even if Layout = null hides them, we keep the backend standard!)
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Contracts", "/Contracts"),
                ("Print Lease", "")
            };

            if (id == null) return NotFound();

            // Rule 1: Try-catch block for safety
            try
            {
                var tenantId = _tenantService.GetTenantId();

                // Get the Landlord/Tenant Name for the signature line
                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
                if (tenant != null && !string.IsNullOrWhiteSpace(tenant.Name))
                {
                    OwnerName = tenant.Name;
                }

                Contract = await _context.Contracts
                    .Include(c => c.Occupant)
                    .Include(c => c.Unit)
                        .ThenInclude(u => u.Building)
                    .Include(c => c.Unit)
                        .ThenInclude(u => u.UnitServices)
                    .Include(c => c.AddOns)
                        .ThenInclude(a => a.ChargeDefinition)
                    .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId);

                if (Contract == null) return NotFound();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load contract {ContractId} for printing.", id);
                TempData["ErrorMessage"] = "A critical error occurred while preparing the document for printing.";
                return RedirectToPage("./Index");
            }
        }
    }
}