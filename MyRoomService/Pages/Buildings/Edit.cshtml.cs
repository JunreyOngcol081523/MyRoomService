using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Buildings
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService; // Re-introducing this

        public EditModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [BindProperty]
        public Building Building { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            // We use IgnoreQueryFilters to bypass the automatic "WHERE TenantId = ..."
            // Then we manually check it to see what's going on.
            var building = await _context.Buildings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (building == null) return Content("Database says this ID does not exist at all.");

            var userTenantId = _tenantService.GetTenantId();

            // This is the moment of truth:
            if (building.TenantId != userTenantId)
            {
                return Content($"Mismatch Detected! DB has '{building.TenantId}', but User has '{userTenantId}'.");
            }

            Building = building;
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Buildings", "/Buildings"),
                (Building.Name, $"/Buildings/ManageBuilding?id={id}"),
                ("Edit Details", $"/Buildings/Edit?id={id}")
            };
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // The hidden TenantId from the form ensures we stay in the right lane.
            // However, as an extra security check, we re-verify it here.
            var currentTenantId = _tenantService.GetTenantId();
            if (Building.TenantId != currentTenantId)
            {
                return Forbid(); // Someone tried to change the TenantId in the hidden field!
            }

            _context.Attach(Building).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Buildings.Any(e => e.Id == Building.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }
    }
}