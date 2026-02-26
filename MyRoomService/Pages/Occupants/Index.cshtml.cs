using Microsoft.AspNetCore.Mvc;
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

        // 1. Add the SearchTerm property
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Set breadcrumbs safely at the top
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
    {
        ("Occupants", "/Occupants")
    };

            try
            {
                var tenantId = _tenantService.GetTenantId();

                // 2. Build the base query 
                // Added "!b.IsArchived" to filter out archived occupants
                var query = _context.Occupants
                        .Include(o => o.Contracts)
                            .ThenInclude(c => c.Unit)
                        .IgnoreQueryFilters()
                        .Where(b => b.TenantId == tenantId && !b.IsArchived) // <-- FIX IS HERE
                        .AsQueryable();

                // 3. Apply the search filter if the user typed something
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var search = SearchTerm.ToLower().Trim();
                    query = query.Where(o =>
                        o.FirstName.ToLower().Contains(search) ||
                        o.LastName.ToLower().Contains(search) ||
                        o.Email.ToLower().Contains(search) ||
                        (o.Phone != null && o.Phone.Contains(search)) ||
                        o.Contracts.Any(c => c.Unit != null && c.Unit.UnitNumber.ToLower().Contains(search))
                    );
                }

                // 4. Execute the query
                Occupants = await query.ToListAsync();
            }
            catch (Exception ex)
            {
                // If you have ILogger injected, you can uncomment the line below:
                // _logger.LogError(ex, "Error loading occupant directory for Tenant {TenantId}", _tenantService.GetTenantId());

                // Initialize as an empty list to prevent "NullReferenceException" on your Razor page
                Occupants = new List<Occupant>();

                // Pass a friendly error message to the UI
                TempData["ErrorMessage"] = "An error occurred while loading the occupant directory. Please try refreshing the page.";
            }
        }
    }
}