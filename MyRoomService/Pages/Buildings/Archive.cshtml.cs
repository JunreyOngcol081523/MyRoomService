using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Buildings
{
    public class ArchiveModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ArchiveModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Building Building { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
                {
                    ("Buildings", "/Buildings")
                };
            // We use IgnoreQueryFilters so we can find the building even if it's already archived
            var building = await _context.Buildings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (building == null) return NotFound();

            Building = building;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var buildingToArchive = await _context.Buildings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (buildingToArchive != null)
            {
                buildingToArchive.IsArchived = true;
                // Alternatively: buildingToArchive.Status = "Archived";

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}