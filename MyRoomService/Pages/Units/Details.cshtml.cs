using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Units
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

        public Unit Unit { get; set; } = default!;

        // --- NEW PROPERTY TO HOLD ACTIVE CONTRACTS ---
        public List<Contract> ActiveContracts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Fetch the Unit
            var unit = await _context.Units
                .Include(u => u.Building)
                .Include(u => u.UnitServices)
                .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId);

            if (unit == null) return NotFound();

            Unit = unit;

            // 2. Fetch Active Contracts for this Unit
            // Note: If you don't have a DbSet<Contract> named Contracts, change this to _context.Set<Contract>()
            ActiveContracts = await _context.Contracts
                .Include(c => c.Occupant) // Bring in the occupant data
                .Where(c => c.UnitId == id
                         && c.TenantId == tenantId
                         && c.Status == ContractStatus.Active)
                .ToListAsync();

            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Buildings", "/Buildings"),
                (Unit.Building?.Name ?? "Building", $"/Buildings/ManageBuilding/{Unit.BuildingId}"),
                ($"Unit {Unit.UnitNumber}", "#")
            };

            return Page();
        }
    }
}