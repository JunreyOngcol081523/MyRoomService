using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Occupants
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

        public List<Occupant> Occupants { get; set; } = new();


        public async Task OnGetAsync()
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Occupants", "/Occupants")
            };

            // Filter by the current Landlady's TenantId
            var tenantId = _tenantService.GetTenantId();
            Occupants = await _context.Occupants
                    .Include(o => o.Contracts) // Left joins table contracts
                    .IgnoreQueryFilters()
                    .Where(b => b.TenantId == tenantId)
                    .ToListAsync();
        }
    }
}
