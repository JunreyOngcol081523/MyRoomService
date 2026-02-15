using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Buildings
{
    public class ManageBuildingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public ManageBuildingModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // We use a public property but remove [BindProperty] for the Details view
        public Building Building { get; set; } = default!;
        public List<Unit> Units { get; set; } = new();
        public List<ChargeDefinition> Charges { get; set; } = new();
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            // 1. What does the Application THINK the current Tenant is?
            var activeTenantId = _tenantService.GetTenantId();

            // 2. Fetch the building bypassing filters to see its real owner
            var building = await _context.Buildings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (building == null) return NotFound();

            // 3. Compare them
            if (building.TenantId != activeTenantId)
            {
                return NotFound(); // Or you could return Forbid() if you want to indicate it's a permissions issue
            }
            // Load the units for this specific building
            Units = await _context.Units
                    .Where(u => u.BuildingId == id && u.TenantId == activeTenantId)
                    .OrderBy(u => u.FloorLevel)
                    .ThenBy(u => u.UnitNumber)
                    .ToListAsync();

            Charges = await _context.ChargeDefinitions
                    .Where(c => c.TenantId == activeTenantId)
                    .ToListAsync();
            Building = building;
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
                {
                    ("Buildings", "/Buildings"),
                    (Building.Name, $"/Buildings/ManageBuilding?id={id}")
                };
            return Page();
        }
        public async Task<IActionResult> OnPostCreateChargeAsync(string Name, decimal DefaultAmount, string ChargeType, Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            var newCharge = new ChargeDefinition
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = Name,
                DefaultAmount = DefaultAmount,
                ChargeType = ChargeType
            };

            _context.ChargeDefinitions.Add(newCharge);
            await _context.SaveChangesAsync();

            // 'id' here is the Building ID from the URL so we stay on the same page
            return RedirectToPage(new { id = id, fragment = "rates" });
        }
        // --- EDIT HANDLER ---
        public async Task<IActionResult> OnPostEditChargeAsync(Guid ChargeId, string Name, decimal DefaultAmount, string ChargeType, Guid id)
        {
            var tenantId = _tenantService.GetTenantId();
            var charge = await _context.ChargeDefinitions
                .FirstOrDefaultAsync(c => c.Id == ChargeId && c.TenantId == tenantId);

            if (charge == null) return NotFound();

            charge.Name = Name;
            charge.DefaultAmount = DefaultAmount;
            charge.ChargeType = ChargeType;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id = id, fragment = "rates" });
        }

        // --- DELETE HANDLER ---
        public async Task<IActionResult> OnPostDeleteChargeAsync(Guid ChargeId, Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            // Safety Check: Check if any Contract is using this ChargeDefinition
            bool isUsed = await _context.ContractAddOns
                .AnyAsync(a => a.ChargeDefinitionId == ChargeId && a.TenantId == tenantId);

            if (isUsed)
            {
                // You can use TempData to show an error message in the UI
                TempData["ErrorMessage"] = "Cannot delete. This charge is currently linked to active contracts.";
                return RedirectToPage(new { id = id, fragment = "rates" });
            }

            var charge = await _context.ChargeDefinitions
                .FirstOrDefaultAsync(c => c.Id == ChargeId && c.TenantId == tenantId);

            if (charge != null)
            {
                _context.ChargeDefinitions.Remove(charge);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = id, fragment = "rates" });
        }
    }
}