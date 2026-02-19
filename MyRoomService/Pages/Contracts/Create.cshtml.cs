using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence; // Adjust if your entities are elsewhere
// using MyRoomService.Services; // Uncomment if you need to import your ITenantService namespace

namespace MyRoomService.Pages.Contracts
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context; // Adjust context name if different
        private readonly ITenantService _tenantService;

        public CreateModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [BindProperty]
        public Contract Contract { get; set; } = new Contract();

        // Dropdown lists for the UI
        public SelectList OccupantsList { get; set; } = default!;
        public SelectList UnitsList { get; set; } = default!;
        // Variable to hold the name for display purposes
        public string OccupantName { get; set; } = string.Empty;

        // Dropdown for Buildings (UI Requirement)
        public SelectList BuildingsList { get; set; } = default!;
        public async Task<IActionResult> OnGetAsync(Guid occupantId)
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Fetch the Occupant details to display their name
            var occupant = await _context.Occupants
                .FirstOrDefaultAsync(o => o.Id == occupantId && o.TenantId == tenantId);

            if (occupant == null)
            {
                return NotFound("Occupant not found or access denied.");
            }

            OccupantName = $"{occupant.FirstName} {occupant.LastName}";
            Contract.OccupantId = occupant.Id; // Bind the ID for the form submission

            // 2. Load Buildings for the dropdown
            var buildings = await _context.Buildings
                .Where(b => b.TenantId == tenantId)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();

            BuildingsList = new SelectList(buildings, "Id", "Name");

            // 3. Set Defaults
            Contract.StartDate = DateTime.Today;
            Contract.BillingDay = 1;
            Contract.Status = ContractStatus.Reserved;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            if (!ModelState.IsValid)
            {
                // If validation fails, we must reload the dropdowns before returning the page
                await OnGetAsync(Contract.OccupantId);
                return Page();
            }

            // Assign the tenant ID to the new contract
            Contract.TenantId = tenantId;

            _context.Contracts.Add(Contract);
            await _context.SaveChangesAsync();

            // Optional: If you track unit availability, you might want to update the Unit's status here to "Occupied"

            return RedirectToPage("./Index");
        }
        public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, ContractStatus newStatus)
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Find the exact contract for this tenant
            var contract = await _context.Contracts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

            if (contract == null)
            {
                return NotFound();
            }

            // 2. Update the status
            contract.Status = newStatus;

            // Optional: If you are setting it to 'Terminated', you might want to auto-set the EndDate
            if (newStatus == ContractStatus.Terminated && contract.EndDate == null)
            {
                contract.EndDate = DateTime.Today;
            }

            // 3. Save to database
            await _context.SaveChangesAsync();

            // 4. Refresh the page to show the updated card colors
            return RedirectToPage("./Index");
        }
    }
}