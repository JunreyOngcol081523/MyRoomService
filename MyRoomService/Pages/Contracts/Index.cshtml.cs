using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Contracts
{
    public class IndexModel : PageModel
    {
        private readonly MyRoomService.Infrastructure.Persistence.ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        //constructor
        public IndexModel(MyRoomService.Infrastructure.Persistence.ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }
        public IList<Domain.Entities.Contract> Contracts { get; set; } = default!;
        public async Task OnGetAsync(Guid? occupantId)
        {
            var activeTenantId = _tenantService.GetTenantId();

            // 1. Build the base query
            var query = _context.Contracts
                .Include(c => c.Unit)
                    .ThenInclude(u => u.Building)
                .Include(c => c.Occupant)
                .Where(c => c.TenantId == activeTenantId);

            // 2. Initialize Breadcrumbs (Matching your specific list format)
            var breadcrumbs = new List<(string Title, string Url)>
            {
                ("Contracts", "/Contracts")
            };

            // 3. Handle Filtering
            if (occupantId.HasValue)
            {
                var occupant = await _context.Occupants.FindAsync(occupantId);
                if (occupant != null)
                {
                    query = query.Where(c => c.OccupantId == occupantId.Value);
                    ViewData["FilterName"] = occupant.FullName;

                    // Append the occupant's name to the trail if filtered
                    breadcrumbs.Add((occupant.FullName, $"/Contracts?occupantId={occupantId}"));
                }
            }

            ViewData["Breadcrumbs"] = breadcrumbs;
            Contracts = await query.ToListAsync();
        }
    }
}
