#nullable disable

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
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool EmailSent { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Always show success even if user not found (security best practice)
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                EmailSent = true;
                return Page();
            }

            // ✅ Generate and encode token properly
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
                "Confirm your email -RentFlow",
                $@"
                <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:40px;background:#f9f9f9;border-radius:10px;'>
                    <div style='background:#ffffff;padding:30px;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>

                        <h2 style='color:#0d6efd;'>Email Confirmation 📧</h2>
                        <p style='color:#555;font-size:14px;line-height:1.6;'>
                            You requested a new confirmation link. Click the button below to confirm your email address.
                        </p>

                        <div style='text-align:center;margin:30px 0;'>
                            <a href='{confirmationLink}'
                               style='display:inline-block;background:#0d6efd;color:#ffffff;
                                      padding:14px 32px;text-decoration:none;border-radius:6px;
                                      font-size:15px;font-weight:bold;'>
                                ✅ Confirm My Email
                            </a>
                        </div>

                        <p style='color:#888;font-size:12px;text-align:center;'>
                            This link will expire in 24 hours.<br/>
                            If you did not request this, you can safely ignore this email.
                        </p>

                    </div>
                    <p style='color:#bbb;font-size:11px;text-align:center;margin-top:20px;'>
                        © RentFlow · Property Management System
                    </p>
                </div>"
            );

            EmailSent = true;
            return Page();
        }
    }
}