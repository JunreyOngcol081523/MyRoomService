using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Occupants
{
    public class ArchivedOccupantsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ArchivedOccupantsModel> _logger;

        public ArchivedOccupantsModel(ApplicationDbContext context, ITenantService tenantService, ILogger<ArchivedOccupantsModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        public List<Occupant> Occupants { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Occupants", "/Occupants"),
                ("Archived", "")
            };

            try
            {
                var tenantId = _tenantService.GetTenantId();

                // Fetch ONLY archived occupants
                var query = _context.Occupants
                        .Include(o => o.Contracts)
                            .ThenInclude(c => c.Unit)
                        .IgnoreQueryFilters()
                        .Where(b => b.TenantId == tenantId && b.IsArchived)
                        .AsQueryable();

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

                Occupants = await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading archived occupants.");
                Occupants = new List<Occupant>();
                TempData["ErrorMessage"] = "An error occurred while loading the archived occupants.";
            }
        }

        // Feature: Allow the user to un-archive an occupant
        public async Task<IActionResult> OnPostRestoreAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();
            var occupant = await _context.Occupants
                .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

            if (occupant != null)
            {
                try
                {
                    occupant.IsArchived = false;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Occupant {OccupantId} was restored from archive.", id);
                    TempData["StatusMessage"] = "Success: Occupant has been restored to the active directory.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore occupant {OccupantId}", id);
                    TempData["ErrorMessage"] = "Error: Could not restore the occupant.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Error: Occupant not found.";
            }

            return RedirectToPage();
        }
    }
}