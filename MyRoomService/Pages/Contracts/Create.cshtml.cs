using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.HelperDTO;
using MyRoomService.Infrastructure.Persistence;
using System.Text.Json;

namespace MyRoomService.Pages.Contracts
{
    public class CreateModel : PageModel
    {
        #region Dependencies & Constructor

        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public CreateModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        #endregion

        #region Bound Properties

        [BindProperty]
        public Contract Contract { get; set; } = new Contract();

        [BindProperty]
        public List<ContractAddOnDto> SelectedAddOns { get; set; } = new();

        [BindProperty]
        public List<IncludedUnitServiceDto> IncludedUnitServices { get; set; } = new();

        #endregion

        #region UI State Properties

        public SelectList OccupantsList { get; set; } = default!;
        public SelectList UnitsList { get; set; } = default!;
        public string OccupantName { get; set; } = string.Empty;
        public SelectList BuildingsList { get; set; } = default!;
        public string AvailableAddOnsJson { get; set; } = "[]";
        public int SpacesAvailable { get; set; }
        #endregion

        #region HTTP GET Handlers

        public async Task<IActionResult> OnGetAsync(Guid occupantId)
        {
            var tenantId = _tenantService.GetTenantId();

            var occupant = await _context.Occupants
                .FirstOrDefaultAsync(o => o.Id == occupantId && o.TenantId == tenantId);

            if (occupant == null)
            {
                return NotFound("Occupant not found or access denied.");
            }

            OccupantName = $"{occupant.FirstName} {occupant.LastName}";
            Contract.OccupantId = occupant.Id;

            var buildings = await _context.Buildings
                .Where(b => b.TenantId == tenantId)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();

            BuildingsList = new SelectList(buildings, "Id", "Name");

            Contract.StartDate = DateTime.Today;
            Contract.BillingDay = 1;
            Contract.Status = ContractStatus.Reserved;

            var addOns = await _context.ChargeDefinitions
                .Where(c => c.TenantId == tenantId)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChargeType = c.ChargeType,
                    DefaultAmount = c.DefaultAmount
                })
                .ToListAsync();

            AvailableAddOnsJson = JsonSerializer.Serialize(addOns);

            return Page();
        }

        public async Task<JsonResult> OnGetUnitsAsync(Guid buildingId)
        {
            var tenantId = _tenantService.GetTenantId();

            var units = await _context.Units
                .Where(u => u.BuildingId == buildingId && u.TenantId == tenantId)
                .Where(u => u.Contracts
                    .Count(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Reserved) < u.MaxOccupancy)
                .Select(u => new
                {
                    value = u.Id,
                    text = u.UnitNumber,
                    spacesAvailable = u.MaxOccupancy - u.Contracts
                        .Count(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Reserved)
                })
                .ToListAsync();

            return new JsonResult(units);
        }

        public async Task<JsonResult> OnGetUnitServicesAsync(Guid unitId)
        {
            var tenantId = _tenantService.GetTenantId();

            var services = await _context.UnitServices
                .Where(us => us.UnitId == unitId && us.TenantId == tenantId)
                .Select(us => new
                {
                    name = us.Name,
                    monthlyPrice = us.MonthlyPrice
                })
                .ToListAsync();

            return new JsonResult(services);
        }

        #endregion

        #region HTTP POST Handlers

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            if (!ModelState.IsValid)
            {
                await OnGetAsync(Contract.OccupantId);
                return Page();
            }

            Contract.TenantId = tenantId;

            // Explicitly generate the Contract ID so we can use it right now
            if (Contract.Id == Guid.Empty)
            {
                Contract.Id = Guid.NewGuid();
            }

            // 1. Process Global Add-ons
            if (SelectedAddOns != null && SelectedAddOns.Any())
            {
                foreach (var addonDto in SelectedAddOns)
                {
                    var newAddOn = new ContractAddOn
                    {
                        TenantId = tenantId,
                        ChargeDefinitionId = addonDto.ChargeDefinitionId,
                        AgreedAmount = addonDto.AgreedAmount
                    };

                    Contract.AddOns.Add(newAddOn);
                }
            }

            // 2. Process the "Snapshot" of Included Unit Services
            if (IncludedUnitServices != null && IncludedUnitServices.Any())
            {
                foreach (var serviceDto in IncludedUnitServices)
                {
                    // Uncomment when ready
                }
            }

            // 3. Fix DateTime Zones for PostgreSQL
            Contract.StartDate = DateTime.SpecifyKind(Contract.StartDate, DateTimeKind.Utc);
            if (Contract.EndDate.HasValue)
            {
                Contract.EndDate = DateTime.SpecifyKind(Contract.EndDate.Value, DateTimeKind.Utc);
            }

            // 🚨 4. THE MAGIC: LINK FLOATING INVOICES TO THIS CONTRACT
            // Find any invoice for this occupant that doesn't have a room assigned yet
            var floatingInvoices = await _context.Invoices
                .Where(i => i.TenantId == tenantId
                         && i.OccupantId == Contract.OccupantId
                         && i.ContractId == null)
                .ToListAsync();

            foreach (var invoice in floatingInvoices)
            {
                invoice.ContractId = Contract.Id;
            }

            // 5. Save everything to the database
            _context.Contracts.Add(Contract);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, ContractStatus newStatus)
        {
            var tenantId = _tenantService.GetTenantId();

            var contract = await _context.Contracts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

            if (contract == null)
            {
                return NotFound();
            }

            contract.Status = newStatus;

            if (newStatus == ContractStatus.Terminated && contract.EndDate == null)
            {
                contract.EndDate = DateTime.Today;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        #endregion

        #region DTOs & Nested Classes

        public class ContractAddOnDto
        {
            public Guid ChargeDefinitionId { get; set; }
            public decimal AgreedAmount { get; set; }
        }

        // Added this to fix potential compile errors from your BindProperty
        public class IncludedUnitServiceDto
        {
            public string Name { get; set; } = string.Empty;
            public decimal MonthlyPrice { get; set; }
        }

        #endregion
    }
}