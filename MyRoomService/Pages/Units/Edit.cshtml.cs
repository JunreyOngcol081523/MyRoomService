using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.HelperDTO;
using MyRoomService.Infrastructure.Persistence;
using System.Text.Json;

namespace MyRoomService.Pages.Units
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public EditModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [BindProperty]
        public Unit Unit { get; set; } = default!;

        public string BuildingName { get; set; } = string.Empty;

        // --- PROPERTIES FOR UNIT SERVICES ---
        public string ExistingServicesJson { get; set; } = "[]";

        [BindProperty]
        public List<UnitServiceInputModel> SelectedServices { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Fetch the Unit and its existing Services
            var unit = await _context.Units
                .Include(u => u.Building)
                .Include(u => u.UnitServices)
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

            if (unit == null) return NotFound();

            Unit = unit;
            BuildingName = unit.Building?.Name ?? "Building";

            // 2. Pass existing services to the frontend so the JS table can render them
            var existingServices = unit.UnitServices?.Select(s => new
            {
                Name = s.Name,
                MonthlyPrice = s.MonthlyPrice
            }).ToList() ?? new();

            ExistingServicesJson = JsonSerializer.Serialize(existingServices);

            SetBreadcrumbs(Unit.BuildingId, BuildingName);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            ModelState.Remove("Unit.Building");
            ModelState.Remove("Unit.UnitServices");

            if (!ModelState.IsValid)
            {
                // Reload necessary data if validation fails
                var building = await _context.Buildings.FindAsync(Unit.BuildingId);
                if (building != null) BuildingName = building.Name;
                SetBreadcrumbs(Unit.BuildingId, BuildingName);
                return Page();
            }

            // 1. Fetch the existing unit from the database WITH its services
            var unitToUpdate = await _context.Units
                .Include(u => u.UnitServices)
                .FirstOrDefaultAsync(u => u.Id == Unit.Id && u.TenantId == tenantId);

            if (unitToUpdate == null) return NotFound();

            // 2. Update the scalar properties
            unitToUpdate.UnitNumber = Unit.UnitNumber;
            unitToUpdate.FloorLevel = Unit.FloorLevel;
            unitToUpdate.RentalMode = Unit.RentalMode;
            unitToUpdate.MaxOccupancy = Unit.MaxOccupancy;
            unitToUpdate.DefaultRate = Unit.DefaultRate;
            unitToUpdate.Status = Unit.Status;

            // 3. Update Services: Clear the old ones and add the new ones
            unitToUpdate.UnitServices.Clear();

            if (SelectedServices != null && SelectedServices.Any())
            {
                foreach (var service in SelectedServices)
                {
                    unitToUpdate.UnitServices.Add(new UnitService
                    {
                        TenantId = tenantId,
                        Name = service.Name,
                        MonthlyPrice = service.MonthlyPrice,
                        IsMetered = service.IsMetered,
                        MeterNumber = service.MeterNumber
                    });
                }
            }

            // 4. Save Changes
            await _context.SaveChangesAsync();

            return RedirectToPage("/Buildings/ManageBuilding", new { id = unitToUpdate.BuildingId });
        }

        private void SetBreadcrumbs(Guid bId, string bName)
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Buildings", "/Buildings"),
                (bName, $"/Buildings/ManageBuilding/{bId}"),
                ("Edit Unit", "#")
            };
        }
    }
}