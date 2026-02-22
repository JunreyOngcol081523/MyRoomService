using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Contracts
{
    public class ChangeStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        private readonly UserManager<ApplicationUser> _userManager;

        public ChangeStatusModel(ApplicationDbContext context, ITenantService tenantService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _tenantService = tenantService;
            _userManager = userManager;
        }

        public Contract Contract { get; set; } = default!;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            var contract = await _context.Contracts
                .Include(c => c.Occupant)
                .Include(c => c.Unit)
                .Include(c => c.AddOns)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

            if (contract == null) return NotFound();

            Contract = contract;
            SetBreadcrumbs();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Fetch the contract again
            var contractToUpdate = await _context.Contracts
                .Include(c => c.Occupant)
                .Include(c => c.Unit)
                .Include(c => c.AddOns)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

            if (contractToUpdate == null) return NotFound();

            Contract = contractToUpdate; // Re-assign for the view in case we return Page()

            // 2. Verify Password (REPLACE THIS WITH YOUR ACTUAL AUTH LOGIC)
            bool isPasswordValid = await VerifyUserPasswordAsync(Password);

            if (!isPasswordValid)
            {
                ModelState.AddModelError("Password", "Incorrect password. Authorization failed.");
                SetBreadcrumbs();
                return Page();
            }

            // 3. Apply the Status Change
            if (contractToUpdate.Status == ContractStatus.Reserved)
            {
                contractToUpdate.Status = ContractStatus.Active;
                // Optional: Set StartDate to today if it's becoming active right now
                // contractToUpdate.StartDate = DateTime.UtcNow; 
            }
            else if (contractToUpdate.Status == ContractStatus.Active)
            {
                contractToUpdate.Status = ContractStatus.Terminated;
                contractToUpdate.EndDate = DateTime.UtcNow; // Record when it was terminated
            }

            // 4. Save to DB
            await _context.SaveChangesAsync();

            // Redirect back to the Contract Details or a List page
            return RedirectToPage("/Contracts/Index");
        }

        private async Task<bool> VerifyUserPasswordAsync(string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(inputPassword))
                return false;

            // 1. Get the currently logged-in user (now strongly typed as ApplicationUser)
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return false; // User session might have expired or is invalid

            // 2. Securely check the password hash against the database
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(currentUser, inputPassword);

            return isPasswordCorrect;
        }

        private void SetBreadcrumbs()
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Contracts", "/Contracts"),
                ("Change Status", "#")
            };
        }
    }
}