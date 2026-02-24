using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyRoomService.Domain.Entities;
using MyRoomService.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace MyRoomService.Infrastructure.Services
{
    // Change from UserClaimsPrincipalFactory<ApplicationUser> 
    // to UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    public class AdditionalUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly ApplicationDbContext _dbContext;

        public AdditionalUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, // Add this
            IOptions<IdentityOptions> optionsAccessor,
            ApplicationDbContext dbContext)
            : base(userManager, roleManager, optionsAccessor) // Pass it here
        {
            _dbContext = dbContext;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // This base call will now correctly include the "Occupant" role claim
            var identity = await base.GenerateClaimsAsync(user);

            if (user.TenantId.HasValue)
            {
                identity.AddClaim(new Claim("TenantId", user.TenantId.Value.ToString()));
            }

            var occupant = _dbContext.Occupants.FirstOrDefault(o => o.IdentityUserId == user.Id);
            if (occupant != null)
            {
                identity.AddClaim(new Claim("FullName", occupant.FullName));
            }

            return identity;
        }
    }
}
