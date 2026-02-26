using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.MeterReadings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ITenantService tenantService, ILogger<IndexModel> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        // --- Filters ---
        [BindProperty(SupportsGet = true)] public Guid? BuildingId { get; set; }
        [BindProperty(SupportsGet = true)] public string? UtilityName { get; set; }
        [BindProperty(SupportsGet = true)] public string? SearchUnit { get; set; }
        public SelectList Buildings { get; set; } = default!;
        public SelectList UtilityTypes { get; set; } = default!;

        // --- Batch Data ---
        [BindProperty] public DateTime ReadingDate { get; set; } = DateTime.Today;
        [BindProperty] public List<MeterEntryItem> Entries { get; set; } = new();

        // --- Rule 5: Pagination ---
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 25;

        public class MeterEntryItem
        {
            public Guid UnitServiceId { get; set; }
            public string UnitNumber { get; set; } = "";
            public string? MeterNumber { get; set; }
            public double PreviousValue { get; set; }
            public double? CurrentValue { get; set; }
            public bool IsReset { get; set; } // The "Gap Year" handler
        }

        public async Task OnGetAsync()
        {
            // Rule 2: Breadcrumbs
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)> { ("Utilities", "/MeterReadings"), ("Batch Entry", "") };

            // Rule 1: Try-Catch
            try
            {
                var tenantId = _tenantService.GetTenantId();

                // Load Filter Options
                var bldgs = await _context.Buildings.Where(b => b.TenantId == tenantId).OrderBy(b => b.Name).ToListAsync();
                Buildings = new SelectList(bldgs, "Id", "Name");

                var utils = await _context.UnitServices.Where(s => s.TenantId == tenantId && s.IsMetered)
                                .Select(s => s.Name).Distinct().ToListAsync();
                UtilityTypes = new SelectList(utils);

                if (BuildingId.HasValue && !string.IsNullOrEmpty(UtilityName))
                {
                    var query = _context.UnitServices
                        .Include(s => s.Unit)
                        .Where(s => s.TenantId == tenantId && s.IsMetered && s.Name == UtilityName && s.Unit!.BuildingId == BuildingId);

                    if (!string.IsNullOrWhiteSpace(SearchUnit))
                        query = query.Where(s => s.Unit!.UnitNumber.Contains(SearchUnit));

                    // Rule 5: Pagination
                    int count = await query.CountAsync();
                    TotalPages = (int)Math.Ceiling(count / (double)PageSize);
                    var services = await query.OrderBy(s => s.Unit!.UnitNumber).Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();

                    foreach (var s in services)
                    {
                        var lastReading = await _context.MeterReadings
                            .Where(m => m.UnitServiceId == s.Id).OrderByDescending(m => m.ReadingDate).FirstOrDefaultAsync();

                        Entries.Add(new MeterEntryItem
                        {
                            UnitServiceId = s.Id,
                            UnitNumber = s.Unit!.UnitNumber,
                            MeterNumber = s.MeterNumber,
                            PreviousValue = lastReading?.CurrentValue ?? 0
                        });
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error loading meter entry grid"); }
        }

        public async Task<IActionResult> OnPostSaveBatchAsync()
        {
            // Rule 1: Try-Catch
            try
            {
                var tenantId = _tenantService.GetTenantId();
                foreach (var entry in Entries.Where(e => e.CurrentValue.HasValue))
                {
                    var reading = new MeterReading
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        UnitServiceId = entry.UnitServiceId,
                        ReadingDate = ReadingDate,
                        PreviousValue = entry.PreviousValue,
                        CurrentValue = entry.CurrentValue!.Value,
                        IsBilled = entry.IsReset, // If reset, mark as billed so it's not charged
                        Notes = entry.IsReset ? "Meter Reset/Baseline" : "Monthly Reading"
                    };
                    _context.MeterReadings.Add(reading);
                }
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Batch readings saved successfully.";
                return RedirectToPage(new { BuildingId, UtilityName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save failed");
                return Page();
            }
        }
    }
}