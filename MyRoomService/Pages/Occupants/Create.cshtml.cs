using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;

        public CreateModel(
            MyRoomService.Infrastructure.Persistence.ApplicationDbContext context,
            ITenantService tenantService,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender)
        {
            _context = context;
            _tenantService = tenantService;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public Occupant Occupant { get; set; } = default!;

        // 🚨 ADDED [BindProperty] so the HTML form can send data to these variables
        [BindProperty]
        public bool GenerateInitialInvoice { get; set; } = true;

        [BindProperty]
        public decimal AdvanceAmount { get; set; }

        [BindProperty]
        public decimal DepositAmount { get; set; }

        public IActionResult OnGet()
        {
            Occupant = new Occupant
            {
                KycStatus = KycStatus.Pending
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var tenantId = _tenantService.GetTenantId();

                // 1. Generate the plain-text password
                string tempPassword = GenerateRandomPassword();

                // 2. Create the Identity User
                var newUser = new ApplicationUser
                {
                    UserName = Occupant.Email,
                    Email = Occupant.Email,
                    EmailConfirmed = true,
                    TenantId = tenantId
                };

                // Identity automatically hashes tempPassword during this call
                var result = await _userManager.CreateAsync(newUser, tempPassword);

                if (result.Succeeded)
                {
                    // 3. Assign Role & Link Domain Entity
                    await _userManager.AddToRoleAsync(newUser, "Occupant");

                    Occupant.TenantId = tenantId;
                    Occupant.Id = Guid.NewGuid();
                    Occupant.IdentityUserId = newUser.Id;

                    _context.Occupants.Add(Occupant);

                    // 🚨 NEW: The Floating Invoice Generator
                    if (GenerateInitialInvoice && (AdvanceAmount > 0 || DepositAmount > 0))
                    {
                        var moveInInvoice = new Invoice
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            OccupantId = Occupant.Id,
                            ContractId = null, // <-- THIS IS THE MAGIC. It floats!
                            InvoiceDate = DateTime.UtcNow,
                            DueDate = DateTime.UtcNow.AddDays(3),
                            Status = "UNPAID",
                            IsPublished = true, // Live immediately so they can pay it
                            TotalAmount = AdvanceAmount + DepositAmount,
                            Items = new List<InvoiceItem>()
                        };

                        if (AdvanceAmount > 0)
                        {
                            moveInInvoice.Items.Add(new InvoiceItem
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                InvoiceId = moveInInvoice.Id,
                                ItemType = "ADVANCE",
                                Description = "Advance Rent Payment",
                                Amount = AdvanceAmount
                            });
                        }

                        if (DepositAmount > 0)
                        {
                            moveInInvoice.Items.Add(new InvoiceItem
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                InvoiceId = moveInInvoice.Id,
                                ItemType = "DEPOSIT",
                                Description = "Security Deposit",
                                Amount = DepositAmount
                            });
                        }

                        _context.Invoices.Add(moveInInvoice);
                    }

                    await _context.SaveChangesAsync();

                    // 4. COMMIT TO DATABASE FIRST
                    await transaction.CommitAsync();

                    // 5. SEND THE WELCOME EMAIL WITH THE PLAIN-TEXT PASSWORD
                    var loginUrl = Url.Page("/Account/Login", pageHandler: null, values: new { area = "Identity" }, protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Occupant.Email,
                        "Welcome to RentFlow Tenant Portal - Login Credentials",
                        $@"
                        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:30px;background:#f9f9f9;border-radius:10px;'>
                            <div style='background:#ffffff;padding:30px;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>
                                <h2 style='color:#0d6efd;margin-top:0;'>Welcome, {Occupant.FirstName}!</h2>
                                <p style='color:#555;font-size:15px;'>Your property manager has created a tenant portal account for you.</p>
                                
                                <div style='background:#f1f6fe;padding:15px;border-left:4px solid #0d6efd;margin:20px 0;'>
                                    <p style='margin:0 0 10px 0;'><strong>Your Login Credentials:</strong></p>
                                    <p style='margin:0 0 5px 0;'><strong>Username:</strong> {Occupant.Email}</p>
                                    <p style='margin:0;'><strong>Password:</strong> <span style='font-family:monospace;font-size:16px;'>{tempPassword}</span></p>
                                </div>

                                <p style='color:#d9534f;font-size:14px;'><em>For your security, please log in and change this temporary password immediately.</em></p>

                                <div style='text-align:center;margin:30px 0;'>
                                    <a href='{loginUrl}' style='display:inline-block;background:#0d6efd;color:#ffffff;padding:12px 25px;text-decoration:none;border-radius:6px;font-weight:bold;'>Go to Login Portal</a>
                                </div>
                            </div>
                        </div>"
                    );

                    TempData["SuccessMessage"] = $"Occupant created! Welcome email sent. (Temp Password: {tempPassword})";
                    return RedirectToPage("./Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while creating the account.");
            }

            return Page();
        }

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

            if (options.RequireLowercase) password += lower[random.Next(lower.Length)];
            if (options.RequireUppercase) password += upper[random.Next(upper.Length)];
            if (options.RequireDigit) password += number[random.Next(number.Length)];
            if (options.RequireNonAlphanumeric) password += special[random.Next(special.Length)];

            string allChars = lower + upper + number + special;
            while (password.Length < length)
            {
                password += allChars[random.Next(allChars.Length)];
            }

            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}