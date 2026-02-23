using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

namespace MyRoomService.Pages.Occupants
{
    public class CreateModel : PageModel
    {
        private readonly MyRoomService.Infrastructure.Persistence.ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(
            MyRoomService.Infrastructure.Persistence.ApplicationDbContext context,
            ITenantService tenantService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _tenantService = tenantService;
            _userManager = userManager;
        }

        [BindProperty]
        public Occupant Occupant { get; set; } = default!;

        public IActionResult OnGet()
        {
            // Initialize with default values
            Occupant = new Occupant
            {
                KycStatus = KycStatus.Pending // Ensure it starts as Pending
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. Generate a secure, random password for the new tenant
            string tempPassword = GenerateRandomPassword();

            // 2. Create the Identity User for the Portal (Removed FirstName/LastName)
            var newUser = new ApplicationUser
            {
                UserName = Occupant.Email, // Using Email as the login username
                Email = Occupant.Email,
                EmailConfirmed = true      // Pre-confirming since the Admin created it
            };

            var result = await _userManager.CreateAsync(newUser, tempPassword);

            if (result.Succeeded)
            {
                // 3. Set the ownership and unique identifiers
                Occupant.TenantId = _tenantService.GetTenantId();
                Occupant.Id = Guid.NewGuid();
                Occupant.IdentityUserId = newUser.Id; // Link the newly generated login account

                // 4. Save the actual Occupant record
                _context.Occupants.Add(Occupant);
                await _context.SaveChangesAsync();

                // 5. Pass the generated credentials to the Index page so you can give them to the tenant
                TempData["SuccessMessage"] = $"Occupant created! Username: {Occupant.Email} | Temporary Password: {tempPassword}";

                return RedirectToPage("./Index");
            }

            // If Identity creation fails (e.g., email already exists)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        // --- Helper Method for Secure Password Generation ---
        private string GenerateRandomPassword()
        {
            var options = _userManager.Options.Password;
            int length = options.RequiredLength < 8 ? 8 : options.RequiredLength;

            string lower = "abcdefghijklmnopqrstuvwxyz";
            string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string number = "1234567890";
            string special = "!@#$%^&*";

            var random = new Random();
            string password = "";

            // Guarantee one of each required character
            if (options.RequireLowercase) password += lower[random.Next(lower.Length)];
            if (options.RequireUppercase) password += upper[random.Next(upper.Length)];
            if (options.RequireDigit) password += number[random.Next(number.Length)];
            if (options.RequireNonAlphanumeric) password += special[random.Next(special.Length)];

            // Fill the rest
            string allChars = lower + upper + number + special;
            while (password.Length < length)
            {
                password += allChars[random.Next(allChars.Length)];
            }

            // Shuffle the characters so the guaranteed ones aren't always first
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}