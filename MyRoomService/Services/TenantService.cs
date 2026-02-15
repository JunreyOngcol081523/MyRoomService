using MyRoomService.Domain.Interfaces;
using System.Security.Claims;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null || !user.Identity.IsAuthenticated)
        {
            return Guid.Empty;
        }

        // 1. Try to get it from Claims (The way we set up with the Factory)
        var claim = user.FindFirst("TenantId");
        if (claim != null && Guid.TryParse(claim.Value, out var tenantId))
        {
            return tenantId;
        }

        // 2. Fallback: If claims are empty (like in your error), 
        // try to find the "sub" or "NameIdentifier" claim which is the User's ID
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);

        // Note: We can't query the database here because it creates a circular dependency
        // So for now, if this is empty, we must ensure the login happened correctly.

        return Guid.Empty;
    }
}