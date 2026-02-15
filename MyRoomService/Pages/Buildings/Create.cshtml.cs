using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Buildings
{
    public class CreateModel : PageModel
    {
        private readonly MyRoomService.Infrastructure.Persistence.ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        public CreateModel(MyRoomService.Infrastructure.Persistence.ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Building Building { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // 1. Grab the ID from the logged-in user's cookie/claims
            var currentTenantId = _tenantService.GetTenantId();

            // 2. Stamp it onto the building
            Building.TenantId = currentTenantId;
            Building.CreatedAt = DateTime.UtcNow;

            _context.Buildings.Add(Building);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
