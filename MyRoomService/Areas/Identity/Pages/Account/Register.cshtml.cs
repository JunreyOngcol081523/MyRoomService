using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using MyRoomService.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MyRoomService.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly Infrastructure.Persistence.ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            Infrastructure.Persistence.ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<ApplicationUser>)userStore;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Business/Organization Name")]
            public string OrganizationName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. Create the Tenant
                    var newTenant = new Tenant
                    {
                        Id = Guid.NewGuid(),
                        Name = Input.OrganizationName,
                        SubscriptionStatus = "ACTIVE"
                    };

                    _context.Add(newTenant);
                    await _context.SaveChangesAsync();

                    // 2. Create the User
                    var user = CreateUser();
                    user.TenantId = newTenant.Id;

                    await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                    // 3. Save the User
                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        await transaction.CommitAsync();
                        _logger.LogInformation("User created a new account and a new Tenant.");

                        // ✅ Generate and ENCODE the token properly
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                        var confirmationLink = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = user.Id, code = encodedToken },
                            protocol: Request.Scheme
                        );

                        await _emailSender.SendEmailAsync(
                            Input.Email,
                            "Confirm your email - MyRoomService",
                            $@"
                            <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:40px;background:#f9f9f9;border-radius:10px;'>
                                <div style='background:#ffffff;padding:30px;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>

                                    <h1 style='color:#0d6efd;font-size:26px;margin-bottom:4px;'>Welcome to MyRoomService! 🎉</h1>
                                    <p style='color:#555;font-size:15px;margin-top:0;'>Your account has been created successfully.</p>

                                    <hr style='border:none;border-top:1px solid #eee;margin:20px 0;'/>

                                    <p style='color:#333;font-size:15px;'>Hi <strong>{Input.OrganizationName}</strong>,</p>
                                    <p style='color:#555;font-size:14px;line-height:1.6;'>
                                        Thank you for registering! Please confirm your email address to activate your account 
                                        and start managing your properties.
                                    </p>

                                    <div style='text-align:center;margin:30px 0;'>
                                        <a href='{confirmationLink}' 
                                           style='display:inline-block;background:#0d6efd;color:#ffffff;
                                                  padding:14px 32px;text-decoration:none;border-radius:6px;
                                                  font-size:15px;font-weight:bold;letter-spacing:0.5px;'>
                                            ✅ Confirm My Email
                                        </a>
                                    </div>

                                    <p style='color:#888;font-size:12px;text-align:center;'>
                                        This link will expire in 24 hours.<br/>
                                        If you did not create an account, you can safely ignore this email.
                                    </p>

                                </div>
                                <p style='color:#bbb;font-size:11px;text-align:center;margin-top:20px;'>
                                    © MyRoomService · Property Management System
                                </p>
                            </div>"
                        );

                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during registration.");
                    ModelState.AddModelError(string.Empty, "A system error occurred. Please try again.");
                }
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'.");
            }
        }
    }
}