using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;
using MyRoomService.Services; // Ensure this matches your namespace

namespace MyRoomService.Pages.Invoices
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IInvoiceService _invoiceService;

        public IndexModel(ApplicationDbContext context, ITenantService tenantService, IInvoiceService invoiceService)
        {
            _context = context;
            _tenantService = tenantService;
            _invoiceService = invoiceService;
        }

        // Your existing properties
        public IList<Invoice> Invoices { get; set; } = default!;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        // --- FILTER PROPERTIES ---
        // [BindProperty(SupportsGet = true)] allows these variables to automatically 
        // catch values from the URL, so you don't have to map them manually!
        [BindProperty(SupportsGet = true)] public string? SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? BuildingId { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? UnitId { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
        [BindProperty] public bool AutoPublish { get; set; }
        // --- PAGINATION PROPERTIES ---
        [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // --- DROPDOWNS ---
        // These hold the list of items for your HTML <select> tags
        public SelectList BuildingOptions { get; set; } = default!;
        public SelectList UnitOptions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            // 1. Load Dropdowns for the filter panel
            var buildings = await _context.Buildings.Where(b => b.TenantId == tenantId).ToListAsync();
            BuildingOptions = new SelectList(buildings, "Id", "Name", BuildingId);

            var unitsQuery = _context.Units.Where(u => u.TenantId == tenantId);
            if (BuildingId.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.BuildingId == BuildingId.Value);
            }
            var units = await unitsQuery.ToListAsync();
            UnitOptions = new SelectList(units, "Id", "UnitNumber", UnitId);

            // 2. Build the Base Query (THIS IS WHERE YOUR CORRECT LOGIC GOES!)
            var query = _context.Invoices
                .Include(i => i.Occupant)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Unit)
                .Where(i => i.TenantId == tenantId && i.Status != "PAID") // Hides completed invoices!
                .AsQueryable();

            // 3. Apply Filters Conditionally
            if (!string.IsNullOrEmpty(SearchName))
            {
                var searchTerm = SearchName.ToLower().Trim();

                // If the user didn't type a wildcard, we automatically wrap it in % 
                // so it acts like a normal partial search.
                if (!searchTerm.Contains("%"))
                {
                    searchTerm = $"%{searchTerm}%";
                }

                // EF.Functions.Like tells the database to evaluate the % and _ symbols as wildcards!
                query = query.Where(i =>
                    i.Occupant != null &&
                    EF.Functions.Like((i.Occupant.FirstName.ToLower() + " " + i.Occupant.LastName.ToLower()), searchTerm)
                );
            }

            if (BuildingId.HasValue)
            {
                query = query.Where(i => i.Contract!.Unit!.BuildingId == BuildingId.Value);
            }

            if (UnitId.HasValue)
            {
                query = query.Where(i => i.Contract!.UnitId == UnitId.Value);
            }

            if (!string.IsNullOrEmpty(StatusFilter))
            {
                query = query.Where(i => i.Status == StatusFilter);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= EndDate.Value);
            }

            // 4. Get Total Count for Pagination Math
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // 5. Apply Pagination and Fetch Data
            Invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.CreatedAt)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            // Run the engine for TODAY's date
            int count = await _invoiceService.GenerateMonthlyInvoicesAsync(tenantId, DateTime.Now, AutoPublish);

            if (count > 0)
            {
                StatusMessage = $"Success! Generated {count} new invoice(s) for today's billing cycle.";
            }
            else
            {
                StatusMessage = "No new invoices needed to be generated today. (Either no active contracts match today's billing day, or they were already billed).";
            }

            return RedirectToPage("./Index");
        }
        public async Task<IActionResult> OnPostPublishAllAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            // Find all invoices for this tenant that are currently Drafts AND Unpaid
            var draftInvoices = await _context.Invoices
                .Where(i => i.TenantId == tenantId && !i.IsPublished && i.Status == "UNPAID")
                .ToListAsync();

            if (draftInvoices.Any())
            {
                int count = draftInvoices.Count;

                foreach (var invoice in draftInvoices)
                {
                    invoice.IsPublished = true;
                }

                await _context.SaveChangesAsync();
                StatusMessage = $"Success! {count} draft invoice(s) have been published and are now visible to occupants.";
            }
            else
            {
                StatusMessage = "No draft, unpaid invoices were found to publish.";
            }

            return RedirectToPage();
        }
    }
}