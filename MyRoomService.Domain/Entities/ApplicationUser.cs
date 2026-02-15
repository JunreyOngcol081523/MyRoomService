using Microsoft.AspNetCore.Identity;

namespace MyRoomService.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        // Changed from int? to Guid?
        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}