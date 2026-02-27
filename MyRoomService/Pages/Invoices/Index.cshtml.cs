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
                            // 🚨 NEW: The Grace Period Check
                            // If the occupant moved in during the current billing month, skip the meter check.
                            // They will be billed their utility split starting next month.
                            var currentMonthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
                            bool isNewMoveIn = contract.StartDate >= currentMonthStart;

                            if (isNewMoveIn)
                            {
                                continue;
                            }

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
        public async Task<IActionResult> OnPostDeleteDraftAsync(Guid invoiceId)
        {
            var tenantId = _tenantService.GetTenantId();

            // 🚨 DEBUG 1: Did the ID actually arrive from the UI?
            _logger.LogWarning("========== DELETE DRAFT TRIGGERED ==========");
            _logger.LogWarning($"Target InvoiceId: {invoiceId}");
            _logger.LogWarning($"Current TenantId: {tenantId}");

            if (invoiceId == Guid.Empty)
            {
                _logger.LogError("FAILURE: The invoiceId arrived empty! The HTML form did not pass the ID correctly.");
                TempData["ErrorMessage"] = "System Error: Missing Invoice ID.";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Fetch the invoice
                var invoice = await _context.Invoices
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Unit)
                            .ThenInclude(u => u.UnitServices)
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.AddOns)
                            .ThenInclude(a => a.ChargeDefinition)
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

                // 🚨 DEBUG 2: Did we find it in the database?
                if (invoice == null)
                {
                    _logger.LogWarning("FAILURE: Invoice was NULL. It either doesn't exist, or the TenantId doesn't match.");
                    TempData["ErrorMessage"] = "Invoice not found.";
                    return RedirectToPage();
                }

                _logger.LogInformation($"SUCCESS: Found Invoice. Status: '{invoice.Status}', IsPublished: {invoice.IsPublished}");

                // 2. Security Check
                if (invoice.Status != "DRAFT" && invoice.IsPublished)
                {
                    _logger.LogWarning("FAILURE: Security block! Attempted to delete a non-draft or published invoice.");
                    TempData["ErrorMessage"] = "Only Draft invoices can be deleted. Published invoices must be Voided.";
                    return RedirectToPage();
                }

                // 3. METER READING ROLLBACK
                var roommateInvoicesExist = await _context.Invoices
                    .AnyAsync(i => i.Contract!.UnitId == invoice.Contract!.UnitId
                                && i.InvoiceDate.Month == invoice.InvoiceDate.Month
                                && i.InvoiceDate.Year == invoice.InvoiceDate.Year
                                && i.Id != invoice.Id
                                && i.Status != "VOID");

                _logger.LogInformation($"Roommate Check: Do other active invoices exist for this room? {roommateInvoicesExist}");

                if (!roommateInvoicesExist && invoice.Contract?.Unit?.UnitServices != null)
                {
                    var meteredServiceIds = invoice.Contract.Unit.UnitServices.Where(s => s.IsMetered).Select(s => s.Id).ToList();
                    int releasedMeters = 0;

                    foreach (var serviceId in meteredServiceIds)
                    {
                        var lastBilledReading = await _context.MeterReadings
                            .Where(m => m.UnitServiceId == serviceId && m.IsBilled)
                            .OrderByDescending(m => m.ReadingDate)
                            .FirstOrDefaultAsync();

                        if (lastBilledReading != null)
                        {
                            lastBilledReading.IsBilled = false;
                            releasedMeters++;
                        }
                    }
                    _logger.LogInformation($"Released {releasedMeters} meter readings back to the pool.");
                }

                // 4. ADD-ON ROLLBACK
                int releasedAddons = 0;
                if (invoice.Contract?.AddOns != null && invoice.Items != null)
                {
                    var addonItems = invoice.Items.Where(item => item.ItemType == "ADDON").ToList();
                    foreach (var item in addonItems)
                    {
                        var matchedAddOn = invoice.Contract.AddOns
                            .FirstOrDefault(a => a.ChargeDefinition != null
                                              && a.ChargeDefinition.Name == item.Description
                                              && a.IsProcessed);

                        if (matchedAddOn != null)
                        {
                            matchedAddOn.IsProcessed = false;
                            releasedAddons++;
                        }
                    }
                }
                _logger.LogInformation($"Released {releasedAddons} One-Time AddOns back to the pool.");

                // 5. Explicitly Delete the Data
                int itemCount = invoice.Items?.Count ?? 0;
                _context.InvoiceItems.RemoveRange(invoice.Items!);
                _context.Invoices.Remove(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogWarning($"SUCCESS: Deleted Invoice and {itemCount} line items.");
                TempData["StatusMessage"] = "Draft invoice successfully deleted and records rolled back.";
                return new JsonResult(new { success = true, message = "Draft deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"CRITICAL SQL ERROR during deletion: {ex.InnerException?.Message ?? ex.Message}");
                TempData["ErrorMessage"] = "A critical error occurred while deleting the invoice.";
                return new JsonResult(new { success = false, message = "Failed to delete the invoice." });
            }

            
        }
    }
}