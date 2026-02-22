using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase.Gotrue;

namespace MyRoomService.Areas.Identity.Pages.Account
{
    public class RegisterConfirmationModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string Email { get; set; }

        public void OnGet()
        {
        }
    }
}
