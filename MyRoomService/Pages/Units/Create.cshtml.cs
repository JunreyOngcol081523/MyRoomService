using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.HelperDTO;
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

        // No more AvailableChargesJson needed here!

        [BindProperty]
        public List<UnitServiceInputModel> SelectedServices { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid buildingId)
        {
            var tenantId = _tenantService.GetTenantId();

            var building = await _context.Buildings
                .FirstOrDefaultAsync(b => b.Id == buildingId && b.TenantId == tenantId);

            if (building == null) return NotFound();

            BuildingName = building.Name;
            BuildingId = buildingId;
            SetBreadcrumbs(buildingId, BuildingName ?? "Building");

            // Removed the ChargeDefinitions query completely.

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            Unit.TenantId = tenantId;
            Unit.BuildingId = BuildingId;

            ModelState.Remove("Unit.Building");
            ModelState.Remove("Unit.UnitServices");

            if (!ModelState.IsValid)
            {
                var building = await _context.Buildings.FindAsync(BuildingId);
                if (building != null) BuildingName = building.Name;
                SetBreadcrumbs(BuildingId, BuildingName ?? "Building");
                return Page();
            }

            if (SelectedServices != null && SelectedServices.Any())
            {
                Unit.UnitServices ??= new List<UnitService>();

                foreach (var service in SelectedServices)
                {
                    Unit.UnitServices.Add(new UnitService
                    {
                        TenantId = tenantId,
                        Name = service.Name,               // Now saving the custom text
                        MonthlyPrice = service.MonthlyPrice, // Now saving the custom price
                        IsMetered = service.IsMetered,  
                        MeterNumber = service.IsMetered ? service.MeterNumber : null // Save meter number if metered
                    });
                }
            }

            _context.Units.Add(Unit);
            await _context.SaveChangesAsync();

            SetBreadcrumbs(Unit.BuildingId, BuildingName ?? "Building");

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