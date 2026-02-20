using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;
using System.Text.Json; // Adjust if your entities are elsewhere
// using MyRoomService.Services; // Uncomment if you need to import your ITenantService namespace

namespace MyRoomService.Pages.Contracts
{
    /// <summary>
    /// Page model responsible for handling the creation of new tenant contracts.
    /// </summary>
    public class CreateModel : PageModel
    {
        #region Dependencies & Constructor

        private readonly ApplicationDbContext _context; // Adjust context name if different
        private readonly ITenantService _tenantService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateModel"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="tenantService">The service used to retrieve the current tenant's ID.</param>
        public CreateModel(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        #endregion

        #region Bound Properties

        /// <summary>
        /// The main contract entity populated from the form submission.
        /// </summary>
        [BindProperty]
        public Contract Contract { get; set; } = new Contract();

        /// <summary>
        /// The list of dynamic add-ons selected by the user in the UI modal.
        /// </summary>
        [BindProperty]
        public List<ContractAddOnDto> SelectedAddOns { get; set; } = new();

        #endregion

        #region UI State Properties

        /// <summary>
        /// Dropdown list for selecting an occupant (if applicable).
        /// </summary>
        public SelectList OccupantsList { get; set; } = default!;

        /// <summary>
        /// Dropdown list for selecting a unit.
        /// </summary>
        public SelectList UnitsList { get; set; } = default!;

        /// <summary>
        /// The display name of the occupant the contract is being created for.
        /// </summary>
        public string OccupantName { get; set; } = string.Empty;

        /// <summary>
        /// Dropdown list for filtering available units by building.
        /// </summary>
        public SelectList BuildingsList { get; set; } = default!;

        /// <summary>
        /// A JSON serialized string of available ChargeDefinitions (Add-ons) used by the frontend JavaScript.
        /// </summary>
        public string AvailableAddOnsJson { get; set; } = "[]";

        #endregion

        #region HTTP GET Handlers

        /// <summary>
        /// Handles the initial GET request to load the create contract page.
        /// </summary>
        /// <param name="occupantId">The unique identifier of the occupant.</param>
        /// <returns>The rendered page or a NotFound result if the occupant doesn't exist.</returns>
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

            // Load Add-ons (ChargeDefinitions) for the modal
            var addOns = await _context.ChargeDefinitions
                .Where(c => c.TenantId == tenantId)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChargeType = c.ChargeType, // Removed .ToString() since it's already a string
                    DefaultAmount = c.DefaultAmount
                })
                .ToListAsync();

            AvailableAddOnsJson = JsonSerializer.Serialize(addOns);

            return Page();
        }

        /// <summary>
        /// An AJAX endpoint called via JavaScript to load units dynamically when a building is selected.
        /// </summary>
        /// <param name="buildingId">The ID of the selected building.</param>
        /// <returns>A JSON array of available units for the selected building.</returns>
        public async Task<JsonResult> OnGetUnitsAsync(Guid buildingId)
        {
            // Adjust "UnitNumber" and "Status" to match your actual Unit entity
            var units = await _context.Units
                .Where(u => u.BuildingId == buildingId && u.TenantId == _tenantService.GetTenantId())
                // Optional: .Where(u => u.Status == UnitStatus.Available)
                .Select(u => new
                {
                    value = u.Id,
                    text = u.UnitNumber
                })
                .ToListAsync();

            return new JsonResult(units);
        }

        #endregion

        #region HTTP POST Handlers

        /// <summary>
        /// Handles the form submission to save the new contract and its add-ons to the database.
        /// </summary>
        /// <returns>A redirect to the index page on success, or redisplays the page with errors.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            if (!ModelState.IsValid)
            {
                // If validation fails, we must reload the dropdowns before returning the page
                await OnGetAsync(Contract.OccupantId);
                return Page();
            }

            // 1. Assign the tenant ID to the main contract
            Contract.TenantId = tenantId;

            // 2. Process the Add-ons (if the user selected any)
            if (SelectedAddOns != null && SelectedAddOns.Any())
            {
                foreach (var addonDto in SelectedAddOns)
                {
                    // Map the DTO to your actual database entity: ContractAddOn
                    var newAddOn = new ContractAddOn
                    {
                        TenantId = tenantId,
                        ChargeDefinitionId = addonDto.ChargeDefinitionId,
                        AgreedAmount = addonDto.AgreedAmount // Matches your domain model!
                    };

                    Contract.AddOns.Add(newAddOn);
                }
            }
            // Force the StartDate to be UTC
            Contract.StartDate = DateTime.SpecifyKind(Contract.StartDate, DateTimeKind.Utc);

            // If an EndDate was provided, force it to be UTC as well
            if (Contract.EndDate.HasValue)
            {
                Contract.EndDate = DateTime.SpecifyKind(Contract.EndDate.Value, DateTimeKind.Utc);
            }
            // 3. Save the Contract AND its Add-ons to the database in one transaction
            _context.Contracts.Add(Contract);
            await _context.SaveChangesAsync();

            // 4. (Optional) If you have a Unit status to update, you can do it here.

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Updates the status of an existing contract. 
        /// Note: This is currently located in the CreateModel, but is typically utilized from a Details or Index page.
        /// </summary>
        /// <param name="id">The contract ID.</param>
        /// <param name="newStatus">The status to apply to the contract.</param>
        /// <returns>A redirect to the index page.</returns>
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

        #endregion

        #region DTOs & Nested Classes

        /// <summary>
        /// Data Transfer Object representing an add-on charge selected for the contract.
        /// </summary>
        public class ContractAddOnDto
        {
            /// <summary>
            /// The foreign key linking to the ChargeDefinition setup for the tenant.
            /// </summary>
            public Guid ChargeDefinitionId { get; set; }

            /// <summary>
            /// The custom amount agreed upon for this specific contract.
            /// </summary>
            public decimal AgreedAmount { get; set; }
        }

        #endregion
    }
}