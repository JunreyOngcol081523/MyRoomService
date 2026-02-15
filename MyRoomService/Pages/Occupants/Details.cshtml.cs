using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;

namespace MyRoomService.Pages.Occupants
{
    public class DetailsModel : PageModel
    {
        private readonly MyRoomService.Infrastructure.Persistence.ApplicationDbContext _context;

        public DetailsModel(MyRoomService.Infrastructure.Persistence.ApplicationDbContext context)
        {
            _context = context;
        }

        public Occupant Occupant { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            Occupant = await _context.Occupants
                .Include(o => o.Contracts) // We'll need this later
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Occupant == null) return NotFound();
            var name = Occupant.FullName;

            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
    {
        ("Occupants", "/Occupants"),
        (name, $"/Occupants/Details?id={id}")
    };
            return Page();
        }

        // HANDLER: Verify
        public async Task<IActionResult> OnPostVerifyAsync(Guid id)
        {
            var occupant = await _context.Occupants.FindAsync(id);
            if (occupant != null)
            {
                occupant.KycStatus = KycStatus.Verified;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }

        // HANDLER: Reject
        public async Task<IActionResult> OnPostRejectAsync(Guid id)
        {
            var occupant = await _context.Occupants.FindAsync(id);
            if (occupant != null)
            {
                occupant.KycStatus = KycStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { id = id });
        }
    }
}