using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Web.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // PRODUCTION RULE: Only inject what is absolutely necessary.
        // We removed UserManager from here to break the circular dependency.
        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetTenantId()
        {
            // We look directly at the "Stamps" on the User's Badge (Claims)
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId");

            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out var tenantId))
            {
                return tenantId;
            }

            return 0; // Return 0 for now; the Global Filter will handle the rest.
        }
    }
}