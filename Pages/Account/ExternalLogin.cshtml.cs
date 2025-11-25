using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitQuest.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync(string provider, string? returnUrl = null)
        {
            // după login, unde vrei să ajungă?
            var redirectUrl = Url.Page("/Index");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, provider);
        }
    }
}
