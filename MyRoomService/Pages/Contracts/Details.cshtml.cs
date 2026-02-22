using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Contracts
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

        public Contract Contract { get; set; }
        public string OwnerName { get; set; } // The Landlord/Company

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            // Fetch everything needed for the legal document
            Contract = await _context.Contracts
                .Include(c => c.Occupant)
                .Include(c => c.Unit)
                    .ThenInclude(u => u.Building)
                .Include(c => c.AddOns)
                    .ThenInclude(a => a.ChargeDefinition)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

            if (Contract == null) return NotFound();


            OwnerName = this.GetTenantName(tenantId);

            return Page();
        }
        //get tenant name for display purposes
        public string GetTenantName(Guid id)
        {
            var tenantId = id;

            // Query the Tenant table, grab just the Name, and return a fallback if not found
            var tenantName = _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.Name)
                .FirstOrDefault();

            return tenantName ?? "Unknown Tenant";
        }
    }
}