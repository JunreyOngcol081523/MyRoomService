using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Invoices
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ITenantService tenantService, IInvoiceService invoiceService, ILogger<IndexModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _invoiceService = invoiceService;
            _logger = logger;
        }

        public IList<Invoice> Invoices { get; set; } = default!;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        // --- FILTER PROPERTIES ---
        [BindProperty(SupportsGet = true)] public string? SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? BuildingId { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? UnitId { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

        // --- PAGINATION PROPERTIES ---
        [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // --- DROPDOWNS ---
        public SelectList BuildingOptions { get; set; } = default!;
        public SelectList UnitOptions { get; set; } = default!;

        // --- BILLING BLOCKER PROPERTIES ---
        public bool IsBillingBlocked { get; set; }
        public List<string> PendingMeterUnits { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("Billing", "/Invoices"),
                ("Invoice Management", "")
            };

            try
            {
                var tenantId = _tenantService.GetTenantId();
                var today = DateTime.Today;

                // 1. EVALUATE BILLING BLOCKER
                var activeContracts = await _context.Contracts
                    .Include(c => c.Unit).ThenInclude(u => u.UnitServices)
                    .Where(c => c.TenantId == tenantId && c.Status == ContractStatus.Active)
                    .ToListAsync();

                foreach (var contract in activeContracts)
                {
                    int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
                    int billingDay = Math.Min(contract.BillingDay, daysInMonth);
                    var targetDate = new DateTime(today.Year, today.Month, billingDay);

                    if (today >= targetDate)
                    {
                        var alreadyBilled = await _context.Invoices.AnyAsync(i =>
                            i.ContractId == contract.Id &&
                            i.InvoiceDate.Month == today.Month &&
                            i.InvoiceDate.Year == today.Year &&
                            i.Status != "VOID");

                        if (!alreadyBilled)
                        {
                            var meteredServices = contract.Unit?.UnitServices?.Where(us => us.IsMetered).ToList() ?? new List<UnitService>();

                            foreach (var service in meteredServices)
                            {
                                var hasUnbilledReading = await _context.MeterReadings
                                    .AnyAsync(mr => mr.UnitServiceId == service.Id && !mr.IsBilled);

                                if (!hasUnbilledReading)
                                {
                                    IsBillingBlocked = true;
                                    PendingMeterUnits.Add($"Unit {contract.Unit!.UnitNumber} ({service.Name})");
                                }
                            }
                        }
                    }
                }

                PendingMeterUnits = PendingMeterUnits.Distinct().Take(10).ToList();

                // 2. LOAD FILTERS & GRID
                var buildings = await _context.Buildings.Where(b => b.TenantId == tenantId).ToListAsync();
                BuildingOptions = new SelectList(buildings, "Id", "Name", BuildingId);

                var unitsQuery = _context.Units.Where(u => u.TenantId == tenantId);
                if (BuildingId.HasValue)
                {
                    unitsQuery = unitsQuery.Where(u => u.BuildingId == BuildingId.Value);
                }
                var units = await unitsQuery.ToListAsync();
                UnitOptions = new SelectList(units, "Id", "UnitNumber", UnitId);

                var query = _context.Invoices
                    .Include(i => i.Occupant)
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Unit)
                    .Where(i => i.TenantId == tenantId && i.Status != "PAID")
                    .AsQueryable();

                if (!string.IsNullOrEmpty(SearchName))
                {
                    var searchTerm = SearchName.ToLower().Trim();
                    query = query.Where(i =>
                        i.Occupant != null &&
                        (i.Occupant.FirstName.ToLower() + " " + i.Occupant.LastName.ToLower()).Contains(searchTerm)
                    );
                }

                if (BuildingId.HasValue) query = query.Where(i => i.Contract!.Unit!.BuildingId == BuildingId.Value);
                if (UnitId.HasValue) query = query.Where(i => i.Contract!.UnitId == UnitId.Value);
                if (!string.IsNullOrEmpty(StatusFilter)) query = query.Where(i => i.Status == StatusFilter);
                if (StartDate.HasValue) query = query.Where(i => i.InvoiceDate >= StartDate.Value);
                if (EndDate.HasValue) query = query.Where(i => i.InvoiceDate <= EndDate.Value);

                TotalCount = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
                if (PageIndex < 1) PageIndex = 1;

                Invoices = await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.CreatedAt)
                    .Skip((PageIndex - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Invoices dashboard.");
                StatusMessage = "An error occurred while loading the data.";
            }
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            try
            {
                var tenantId = _tenantService.GetTenantId();

                // 🚨 FORCED DRAFT: 'AutoPublish' is removed. Third parameter is strictly 'false'.
                int count = await _invoiceService.GenerateMonthlyInvoicesAsync(tenantId, DateTime.Now, false);

                if (count > 0)
                {
                    StatusMessage = $"Success! Generated {count} new invoice(s) as Drafts for your review.";
                }
                else
                {
                    StatusMessage = "No new invoices were required for today's cycle.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run billing cycle.");
                StatusMessage = "Critical error occurred during invoice generation.";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostPublishAllAsync()
        {
            try
            {
                var tenantId = _tenantService.GetTenantId();

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish drafts.");
                StatusMessage = "An error occurred while publishing drafts.";
            }

            return RedirectToPage();
        }
    }
}