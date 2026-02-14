using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;
namespace MyRoomService.Pages.Buildings
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ITenantService tenantService)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Building Building { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Get the ID from the Cookie Service (Fast!)
            var currentTenantId = _tenantService.GetTenantId();

            // 2. Critical Check: If the cookie doesn't have an ID, they aren't a valid tenant
            if (currentTenantId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid Tenant Session. Please log in again.");
                return Page();
            }

            // 3. Assign the Stamp
            Building.TenantId = currentTenantId;

            // 4. Remove validation for the hidden field
            ModelState.Remove("Building.TenantId");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Buildings.Add(Building);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
