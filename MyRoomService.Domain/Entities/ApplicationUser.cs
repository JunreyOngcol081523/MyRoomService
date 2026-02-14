using Microsoft.AspNetCore.Identity;

namespace MyRoomService.Domain.Entities
{
    // This class extends the default IdentityUser to add SaaS functionality
    public class ApplicationUser : IdentityUser
    {
        // This links the person logging in to their specific Business/Tenant
        public int? TenantId { get; set; }

        // Navigation property
        public Tenant? Tenant { get; set; }
    }
}
