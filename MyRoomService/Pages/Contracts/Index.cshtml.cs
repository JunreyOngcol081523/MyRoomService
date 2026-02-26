using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Contracts
{
    public class IndexModel : PageModel
    {
        private readonly MyRoomService.Infrastructure.Persistence.ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public IndexModel(MyRoomService.Infrastructure.Persistence.ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IList<Contract> Contracts { get; set; } = default!;
        public Guid? CurrentOccupantId { get; set; }

        // Filter Properties
        [BindProperty(SupportsGet = true)]
        public string? SearchOccupant { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? FilterBuildingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? FilterUnitId { get; set; }

        [BindProperty(SupportsGet = true)]
        public ContractStatus? FilterStatus { get; set; }

        // Dropdown Lists
        public SelectList Buildings { get; set; } = default!;
        public SelectList Units { get; set; } = default!;
        public SelectList Statuses { get; set; } = default!;

        public async Task OnGetAsync(Guid? occupantId)
        {
            CurrentOccupantId = occupantId;
            var activeTenantId = _tenantService.GetTenantId();

            var breadcrumbs = new List<(string Title, string Url)>
            {
                ("Contracts", "/Contracts")
            };

            // 1. Build the base query
            var query = _context.Contracts
                .Include(c => c.Unit)
                    .ThenInclude(u => u.Building)
                .Include(c => c.Occupant)
                .Where(c => c.TenantId == activeTenantId);

            // 2. Handle Occupant Filtering (from URL route)
            if (occupantId.HasValue)
            {
                var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.Id == occupantId.Value && o.TenantId == activeTenantId);
                if (occupant != null)
                {
                    query = query.Where(c => c.OccupantId == occupantId.Value);
                    ViewData["FilterName"] = occupant.FirstName + " " + occupant.LastName;
                    breadcrumbs.Add(($"{occupant.FirstName} {occupant.LastName}", $"/Contracts?occupantId={occupantId}"));
                }
            }

            // 3. Apply Form Filters
            if (!string.IsNullOrWhiteSpace(SearchOccupant))
            {
                var search = SearchOccupant.ToLower().Trim();
                // EF Core translates this to SQL ILIKE or similar
                query = query.Where(c =>
                    c.Occupant != null && (
                        c.Occupant.FirstName.ToLower().Contains(search) ||
                        c.Occupant.LastName.ToLower().Contains(search)
                    )
                );
            }

            if (FilterBuildingId.HasValue)
            {
                query = query.Where(c => c.Unit != null && c.Unit.BuildingId == FilterBuildingId.Value);
            }

            if (FilterUnitId.HasValue)
            {
                query = query.Where(c => c.UnitId == FilterUnitId.Value);
            }

            if (FilterStatus.HasValue)
            {
                query = query.Where(c => c.Status == FilterStatus.Value);
            }

            // 4. Populate Dropdowns
            var buildings = await _context.Buildings
                .Where(b => b.TenantId == activeTenantId)
                .OrderBy(b => b.Name)
                .ToListAsync();
            Buildings = new SelectList(buildings, "Id", "Name");

            var unitsQuery = _context.Units.Where(u => u.TenantId == activeTenantId);

            // If a building is selected, restrict the units dropdown to only units in that building
            if (FilterBuildingId.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.BuildingId == FilterBuildingId.Value);
            }

            var units = await unitsQuery.OrderBy(u => u.UnitNumber).ToListAsync();
            Units = new SelectList(units, "Id", "UnitNumber");

            var statuses = Enum.GetValues(typeof(ContractStatus))
                               .Cast<ContractStatus>()
                               .Select(s => new { Id = (int)s, Name = s.ToString() })
                               .ToList();
            Statuses = new SelectList(statuses, "Id", "Name");

            ViewData["Breadcrumbs"] = breadcrumbs;
            Contracts = await query.OrderByDescending(c => c.StartDate).ToListAsync();
        }
    }
}