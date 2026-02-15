using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Occupants
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

        [BindProperty]
        public Occupant Occupant { get; set; } = default!;

        public IActionResult OnGet()
        {
            // Initialize with default values if necessary
            Occupant = new Occupant
            {
                KycStatus = KycStatus.Pending // Ensure it starts as Pending
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Set the ownership and unique identifiers
            Occupant.TenantId = _tenantService.GetTenantId();
            Occupant.Id = Guid.NewGuid();

            _context.Occupants.Add(Occupant);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}