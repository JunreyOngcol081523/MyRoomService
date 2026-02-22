using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyRoomService.Pages
{
    public class AboutModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Breadcrumbs"] = new List<(string Title, string Url)>
            {
                ("About", "/About")
            };
        }
    }
}
