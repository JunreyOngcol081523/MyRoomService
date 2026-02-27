using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.HelperDTO;
using MyRoomService.Infrastructure.Persistence;
using System.Globalization;

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

        // --- NEW: List for Autosuggest Dropdown ---
        public List<string> CommonServiceNames { get; set; } = new();

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

            // Fetch unique service names for the Autosuggest Datalist
            CommonServiceNames = await _context.UnitServices
                .Where(us => us.TenantId == tenantId)
                .Select(us => us.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var tenantId = _tenantService.GetTenantId();

                // 1. Explicitly assign IDs that aren't in the form
                Unit.TenantId = tenantId;
                Unit.BuildingId = BuildingId;

                // 2. Clean up Model State
                ModelState.Remove("Unit.Building");
                ModelState.Remove("Unit.UnitServices");
                // Ensure the new enum property is validated correctly

                var dynamicKeys = ModelState.Keys.Where(k => k.StartsWith("SelectedServices")).ToList();
                foreach (var key in dynamicKeys) ModelState.Remove(key);

                if (!ModelState.IsValid)
                {
                    await ReloadPageDataAsync(tenantId);
                    return Page();
                }

                // 3. Handle Child Services (HashSet/Sync logic for new records)
                if (SelectedServices != null && SelectedServices.Any())
                {
                    Unit.UnitServices ??= new List<UnitService>();
                    var textInfo = CultureInfo.CurrentCulture.TextInfo;

                    foreach (var service in SelectedServices.Where(s => !string.IsNullOrWhiteSpace(s.Name)))
                    {
                        Unit.UnitServices.Add(new UnitService
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            Name = textInfo.ToTitleCase(service.Name.Trim().ToLower()),
                            MonthlyPrice = service.MonthlyPrice,
                            IsMetered = service.IsMetered,
                            MeterNumber = service.IsMetered ? service.MeterNumber : null
                        });
                    }
                }

                // 4. Save the tracked Unit (Includes MeteredBillingMode automatically via binding)
                _context.Units.Add(Unit);
                await _context.SaveChangesAsync();

                TempData["StatusMessage"] = "Unit created successfully.";
                return RedirectToPage("/Buildings/ManageBuilding", new { id = BuildingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "A critical error occurred: " + ex.Message);
                await ReloadPageDataAsync(_tenantService.GetTenantId());
                return Page();
            }
        }

        // Helper to safely reload UI components if a save fails
        private async Task ReloadPageDataAsync(Guid tenantId)
        {
            var building = await _context.Buildings.FindAsync(BuildingId);
            if (building != null) BuildingName = building.Name;

            CommonServiceNames = await _context.UnitServices
                .Where(us => us.TenantId == tenantId)
                .Select(us => us.Name).Distinct().ToListAsync();

            SetBreadcrumbs(BuildingId, BuildingName ?? "Building");
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