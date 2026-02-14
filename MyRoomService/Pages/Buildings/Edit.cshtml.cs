using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Buildings
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Building Building { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var building = await _context.Buildings.FirstOrDefaultAsync(m => m.Id == id);
            if (building == null)
            {
                return NotFound();
            }
            Building = building;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. We remove TenantId from validation because it won't be in the form
            ModelState.Remove("Building.TenantId");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 2. Fetch the 'Version from the Database' to make sure we don't lose the TenantId
            var buildingToUpdate = await _context.Buildings.FirstOrDefaultAsync(m => m.Id == Building.Id);

            if (buildingToUpdate == null)
            {
                return NotFound();
            }

            // 3. Only update the fields the user is allowed to change
            buildingToUpdate.Name = Building.Name;
            buildingToUpdate.Address = Building.Address;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BuildingExists(Building.Id))
                {
                    return NotFound();
                }
                else { throw; }
            }

            return RedirectToPage("./Index");
        }

        private bool BuildingExists(int id)
        {
            return _context.Buildings.Any(e => e.Id == id);
        }
    }
}
