using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Buildings
{
    public class ManageBuildingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public ManageBuildingModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // We use a public property but remove [BindProperty] for the Details view
        public Building Building { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            // 1. What does the Application THINK the current Tenant is?
            var activeTenantId = _tenantService.GetTenantId();

            // 2. Fetch the building bypassing filters to see its real owner
            var building = await _context.Buildings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (building == null) return NotFound();

            // 3. Compare them
            if (building.TenantId != activeTenantId)
            {
                return Content($@"
            <div style='color:red'>
                <h2>Tenant Mismatch Found</h2>
                <p>App thinks Tenant is: <b>{activeTenantId}</b></p>
                <p>Building actually belongs to: <b>{building.TenantId}</b></p>
                <p><i>This is why the Global Filter is hiding it!</i></p>
            </div>", "text/html");
            }

            Building = building;
            return Page();
        }
    }
}