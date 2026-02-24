using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.MeterReadings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Filters
        [BindProperty(SupportsGet = true)]
        public Guid? BuildingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ServiceName { get; set; }

        // Data for Dropdowns
        public SelectList Buildings { get; set; }
        public List<string> ServiceTypes { get; set; }

        // The Entry List
        [BindProperty]
        public List<MeterEntryViewModel> Entries { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 1. Load Buildings for filter
            var buildingList = await _context.Buildings.OrderBy(b => b.Name).ToListAsync();
            Buildings = new SelectList(buildingList, "Id", "Name");

            // 2. Get unique names of metered services (e.g., Electricity, Water)
            ServiceTypes = await _context.UnitServices
                .Where(s => s.IsMetered)
                .Select(s => s.Name)
                .Distinct()
                .ToListAsync();

            if (BuildingId.HasValue && !string.IsNullOrEmpty(ServiceName))
            {
                // 3. Find all units in that building that have this metered service
                var unitServices = await _context.UnitServices
                    .Include(s => s.Unit)
                    .ThenInclude(u => u.Contracts.Where(c => c.Status == ContractStatus.Active))
                    .Where(s => s.Unit.BuildingId == BuildingId && s.Name == ServiceName && s.IsMetered)
                    .ToListAsync();

                foreach (var service in unitServices)
                {
                    // 4. Find the most recent reading for this service to get the PreviousValue
                    var lastReading = await _context.MeterReadings
                        .Where(mr => mr.UnitServiceId == service.Id)
                        .OrderByDescending(mr => mr.ReadingDate)
                        .FirstOrDefaultAsync();

                    Entries.Add(new MeterEntryViewModel
                    {
                        UnitServiceId = service.Id,
                        UnitName = service.Unit?.UnitNumber ?? "Unknown",
                        OccupantCount = service.Unit?.Contracts.FirstOrDefault()?.OccupantId != null ? 1 : 0, // Simplified for now
                        PreviousValue = lastReading?.CurrentValue ?? 0,
                        Rate = service.MonthlyPrice,
                        MeterNumber = service.MeterNumber ?? "N/A"
                    });
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Entries.Any()) return Page();

            foreach (var entry in Entries)
            {
                if (entry.CurrentValue > 0) // Only save if a reading was entered
                {
                    var reading = new MeterReading
                    {
                        UnitServiceId = entry.UnitServiceId,
                        PreviousValue = entry.PreviousValue,
                        CurrentValue = entry.CurrentValue,
                        ReadingDate = DateTime.Now,
                        IsBilled = false
                    };
                    _context.MeterReadings.Add(reading);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index", new { BuildingId, ServiceName, success = true });
        }
    }

    public class MeterEntryViewModel
    {
        public Guid UnitServiceId { get; set; }
        public string UnitName { get; set; }
        public string MeterNumber { get; set; }
        public int OccupantCount { get; set; }
        public double PreviousValue { get; set; }
        public double CurrentValue { get; set; }
        public decimal Rate { get; set; }
    }
}