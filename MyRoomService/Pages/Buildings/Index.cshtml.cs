using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Buildings
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

        public IList<Building> Building { get; set; } = default!;


        public async Task OnGetAsync()

        {

            var debugId = _tenantService.GetTenantId();



            Building = await _context.Buildings

                    .IgnoreQueryFilters()

                    .Where(b => b.TenantId == debugId && !b.IsArchived)

                    .ToListAsync();

        }

    }
}
