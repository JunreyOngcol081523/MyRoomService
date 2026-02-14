using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyRoomService.Domain.Entities;
using System.Security.Claims;

namespace MyRoomService.Web.Services
{
    // This class is responsible for adding extra "stamps" (Claims) to the user's cookie
    public class AdditionalUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public AdditionalUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // 1. Generate the standard claims (Name, Email, etc.)
            var identity = await base.GenerateClaimsAsync(user);

            // 2. Add our custom "TenantId" stamp
            // We store it as a string because Claims are always text-based
            if (user.TenantId.HasValue)
            {
                identity.AddClaim(new Claim("TenantId", user.TenantId.Value.ToString()));
            }

            return identity;
        }
    }
}