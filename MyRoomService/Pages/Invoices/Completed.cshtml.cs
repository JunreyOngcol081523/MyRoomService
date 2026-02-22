using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Invoices
{
    public class CompletedModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public CompletedModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IList<Invoice> Invoices { get; set; } = default!;

        // --- FILTER PROPERTIES (No StatusFilter needed) ---
        [BindProperty(SupportsGet = true)] public string? SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? BuildingId { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? UnitId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

        // --- PAGINATION PROPERTIES ---
        [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public SelectList BuildingOptions { get; set; } = default!;
        public SelectList UnitOptions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            // Load Dropdowns
            var buildings = await _context.Buildings.Where(b => b.TenantId == tenantId).ToListAsync();
            BuildingOptions = new SelectList(buildings, "Id", "Name", BuildingId);

            var unitsQuery = _context.Units.Where(u => u.TenantId == tenantId);
            if (BuildingId.HasValue) unitsQuery = unitsQuery.Where(u => u.BuildingId == BuildingId.Value);
            var units = await unitsQuery.ToListAsync();
            UnitOptions = new SelectList(units, "Id", "UnitNumber", UnitId);

            // Base Query: ONLY PAID INVOICES
            var query = _context.Invoices
                .Include(i => i.Occupant)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Unit)
                .Where(i => i.TenantId == tenantId && i.Status == "PAID")
                .AsQueryable();

            // Apply Filters
            if (!string.IsNullOrEmpty(SearchName))
                query = query.Where(i => i.Occupant!.FirstName.Contains(SearchName) || i.Occupant!.LastName.Contains(SearchName));
            if (BuildingId.HasValue)
                query = query.Where(i => i.Contract!.Unit!.BuildingId == BuildingId.Value);
            if (UnitId.HasValue)
                query = query.Where(i => i.Contract!.UnitId == UnitId.Value);
            if (StartDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= StartDate.Value);
            if (EndDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= EndDate.Value);

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            Invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}