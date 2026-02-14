using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRoomService.Domain.Entities;
using MyRoomService.Infrastructure.Persistence;

namespace MyRoomService.Pages.Buildings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Building> Building { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // 1. Check the Cookie directly
            var cookieId = User.FindFirst("TenantId")?.Value;

            Building = await _context.Buildings.ToListAsync();
        }
    }
}
