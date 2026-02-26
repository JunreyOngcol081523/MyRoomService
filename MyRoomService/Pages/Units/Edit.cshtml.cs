using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.HelperDTO;
using MyRoomService.Infrastructure.Persistence;
using System.Globalization;
using System.Text.Json;

namespace MyRoomService.Pages.Units
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ITenantService tenantService, ILogger<EditModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        [BindProperty]
        public Unit Unit { get; set; } = default!;
        public string BuildingName { get; set; } = string.Empty;
        public string ExistingServicesJson { get; set; } = "[]";
        public List<string> CommonServiceNames { get; set; } = new();

        [BindProperty]
        public List<UnitServiceInputModel> SelectedServices { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();
            var unit = await _context.Units
                .Include(u => u.Building)
                .Include(u => u.UnitServices)
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

            if (unit == null) return NotFound();

            Unit = unit;
            BuildingName = unit.Building?.Name ?? "Building";

            CommonServiceNames = await _context.UnitServices
                .Where(us => us.TenantId == tenantId)
                .Select(us => us.Name).Distinct().OrderBy(n => n).ToListAsync();

            var currentServices = unit.UnitServices?.Select(s => new {
                Id = s.Id,
                Name = s.Name,
                MonthlyPrice = s.MonthlyPrice,
                IsMetered = s.IsMetered,
                MeterNumber = s.MeterNumber
            }).ToList() ?? new();

            ExistingServicesJson = JsonSerializer.Serialize(currentServices);
            SetBreadcrumbs(Unit.BuildingId, BuildingName);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Sanitize model binding pollution
            Unit.UnitServices?.Clear();
            ModelState.Remove("Unit.Building");
            ModelState.Remove("Unit.UnitServices");
            ModelState.Remove("Unit.TenantId");
            foreach (var key in ModelState.Keys
                .Where(k => k.StartsWith("SelectedServices")).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                await ReloadPageDataAsync(tenantId);
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Fetch Unit WITH tracking (no AsNoTracking)
                // Strict tenant check — no Guid.Empty escape hatch
                var unitToUpdate = await _context.Units
                    .FirstOrDefaultAsync(u => u.Id == Unit.Id && u.TenantId == tenantId);

                if (unitToUpdate == null) return NotFound();

                // 3. ✅ Explicit mapping only — TenantId and BuildingId intentionally excluded
                unitToUpdate.UnitNumber = Unit.UnitNumber;
                unitToUpdate.FloorLevel = Unit.FloorLevel;
                unitToUpdate.RentalMode = Unit.RentalMode;
                unitToUpdate.MaxOccupancy = Unit.MaxOccupancy;
                unitToUpdate.DefaultRate = Unit.DefaultRate;
                unitToUpdate.Status = Unit.Status;

                // 4. Fetch services separately (Gemini's clean pattern — avoids nav collection issues)
                var dbServices = await _context.UnitServices
                    .Where(s => s.UnitId == Unit.Id)
                    .ToListAsync();

                if (SelectedServices == null) SelectedServices = new();
                var textInfo = CultureInfo.CurrentCulture.TextInfo;

                var incomingIds = SelectedServices
                    .Where(s => s.Id.HasValue && s.Id != Guid.Empty)
                    .Select(s => s.Id!.Value)
                    .ToList();

                // 5. Delete orphans (RemoveRange on tracked entities is safe)
                var toDelete = dbServices
                    .Where(s => !incomingIds.Contains(s.Id))
                    .ToList();
                _context.UnitServices.RemoveRange(toDelete);

                // 6. Add or Update — dbServices acts as the confirmed ID guard
                foreach (var incoming in SelectedServices
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name)))
                {
                    var standardizedName = textInfo
                        .ToTitleCase(incoming.Name.Trim().ToLower());

                    // Only trusts IDs confirmed to exist in DB right now
                    var existing = dbServices
                        .FirstOrDefault(s => s.Id == incoming.Id);

                    if (existing != null)
                    {
                        existing.Name = standardizedName;
                        existing.MonthlyPrice = incoming.MonthlyPrice;
                        existing.IsMetered = incoming.IsMetered;
                        existing.MeterNumber = incoming.MeterNumber;
                        // EF tracks this automatically — no manual EntityState needed
                    }
                    else
                    {
                        _context.UnitServices.Add(new UnitService
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            UnitId = unitToUpdate.Id,
                            Name = standardizedName,
                            MonthlyPrice = incoming.MonthlyPrice,
                            IsMetered = incoming.IsMetered,
                            MeterNumber = incoming.MeterNumber
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["StatusMessage"] = "Unit and services updated successfully.";
                return RedirectToPage("/Buildings/ManageBuilding",
                    new { id = unitToUpdate.BuildingId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty,
                    "Save failed: " + (ex.InnerException?.Message ?? ex.Message));
                await ReloadPageDataAsync(tenantId);
                return Page();
            }
        }

        private async Task ReloadPageDataAsync(Guid tenantId)
        {
            var unit = await _context.Units.Include(u => u.UnitServices).FirstOrDefaultAsync(u => u.Id == Unit.Id);
            var building = await _context.Buildings.FindAsync(Unit.BuildingId);
            BuildingName = building?.Name ?? "Building";

            CommonServiceNames = await _context.UnitServices
                .Where(us => us.TenantId == tenantId)
                .Select(us => us.Name).Distinct().ToListAsync();

            var currentServices = unit?.UnitServices?.Select(s => new {
                Id = s.Id,
                Name = s.Name,
                MonthlyPrice = s.MonthlyPrice,
                IsMetered = s.IsMetered,
                MeterNumber = s.MeterNumber
            }).ToList() ?? new();

            ExistingServicesJson = JsonSerializer.Serialize(currentServices);
            SetBreadcrumbs(Unit.BuildingId, BuildingName);
        }

        private void SetBreadcrumbs(Guid bId, string bName) =>
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)> { ("Buildings", "/Buildings"), (bName, $"/Buildings/ManageBuilding/{bId}"), ("Edit Unit", "#") };
    }
}