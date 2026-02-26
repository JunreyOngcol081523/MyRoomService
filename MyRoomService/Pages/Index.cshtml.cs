using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;
using MyRoomService.Infrastructure.Persistence;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context, ITenantService tenantService)
    {
        _userManager = userManager;
        _context = context;
        _tenantService = tenantService;
    }

    public string BusinessName { get; set; }

    // Dashboard Metrics
    public decimal MonthlyCollection { get; set; }
    public decimal TotalCollectibles { get; set; }
    public int UnpaidOccupantsCount { get; set; }
    public int TotalOccupants { get; set; }
    public int TotalBuildings { get; set; }
    public double CollectionEfficiency { get; set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            // Later we will fetch the Tenant Name here!
            BusinessName = "Your SaaS Dashboard";

            var tenantId = _tenantService.GetTenantId();
            var now = DateTime.Now; // Or DateTime.UtcNow if your DB requires it
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            // 1. Physical Assets
            TotalOccupants = await _context.Occupants.CountAsync(o => o.TenantId == tenantId);
            TotalBuildings = await _context.Buildings.CountAsync(b => b.TenantId == tenantId);

            // 2. Financials - Current Month Collection
            // Exclude VOID just in case a voided invoice had a partial payment attached
            MonthlyCollection = await _context.Invoices
                .Where(i => i.TenantId == tenantId
                         && i.InvoiceDate >= firstDayOfMonth
                         && i.Status != "VOID")
                .SumAsync(i => i.AmountPaid);

            // 3. Financials - Total Unpaid (Collectibles)
            // Explicitly look for UNPAID to exclude VOID and PAID
            TotalCollectibles = await _context.Invoices
                .Where(i => i.TenantId == tenantId && i.Status == "UNPAID")
                .SumAsync(i => i.TotalAmount - i.AmountPaid);

            // 4. Delinquency - Count unique occupants with outstanding balances
            UnpaidOccupantsCount = await _context.Invoices
                .Where(i => i.TenantId == tenantId && i.Status == "UNPAID")
                .Select(i => i.OccupantId)
                .Distinct()
                .CountAsync();

            // 5. Collection Efficiency Rate (Paid vs Total Invoiced this month)
            // MUST exclude VOID invoices here so they don't count against your efficiency rate
            var totalInvoicedThisMonth = await _context.Invoices
                .Where(i => i.TenantId == tenantId
                         && i.InvoiceDate >= firstDayOfMonth
                         && i.Status != "VOID")
                .SumAsync(i => i.TotalAmount);

            CollectionEfficiency = totalInvoicedThisMonth > 0
                ? (double)(MonthlyCollection / totalInvoicedThisMonth) * 100
                : 0;
        }
    }
}