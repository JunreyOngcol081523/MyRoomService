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
        public string CurrentStatus { get; set; } = "Active";

        public async Task OnGetAsync(string status = "Active")
        {


            CurrentStatus = status;
            var debugId = _tenantService.GetTenantId();

            if (_context.Buildings != null)
            {
                // 1. Build the base query (Notice we don't use 'await' or 'ToListAsync' yet)
                var baseQuery = _context.Buildings
                    .Include(b => b.Units)
                    .IgnoreQueryFilters()
                    .Where(b => b.TenantId == debugId);

                // 2. Add the specific Active/Archived filter based on the parameter
                if (status == "Archived")
                {
                    ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
                {
                    ("Buildings", "/Archived Buildings")
                };
                    baseQuery = baseQuery.Where(b => b.IsArchived == true);
                }
                else
                {
                    ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
                {
                    ("Buildings", "/Buildings")
                };
                    baseQuery = baseQuery.Where(b => b.IsArchived == false); // Default to Active
                }

                // 3. Execute the query ONCE at the very end
                Building = await baseQuery.ToListAsync();
            }
        }

    }
}
