using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Units
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public CreateModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [BindProperty]
        public Unit Unit { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public Guid BuildingId { get; set; }

        public string BuildingName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(Guid buildingId)
        {
            var tenantId = _tenantService.GetTenantId();

            // Verify building exists and belongs to this tenant
            var building = await _context.Buildings
                .FirstOrDefaultAsync(b => b.Id == buildingId && b.TenantId == tenantId);

            if (building == null) return NotFound();

            BuildingName = building.Name;
            BuildingId = buildingId;
            SetBreadcrumbs(buildingId, BuildingName ?? "Building");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Manually assign the critical IDs
            Unit.TenantId = tenantId;
            Unit.BuildingId = BuildingId;

            // 2. Validate
            if (!ModelState.IsValid) return Page();

            _context.Units.Add(Unit);
            await _context.SaveChangesAsync();
            SetBreadcrumbs(Unit.BuildingId, BuildingName ?? "Building");
            // Redirect back to the Building Management page
            return RedirectToPage("/Buildings/ManageBuilding", new { id = BuildingId });
        }
        private void SetBreadcrumbs(Guid bId, string bName)
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Buildings", "/Buildings"),
                (bName, $"/Buildings/ManageBuilding/{bId}"),
                ("Add New Unit", "#")
            };
        }
    }
}