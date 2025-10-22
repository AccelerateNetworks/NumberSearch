using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger) : PageModel
    {
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost(string returnUrl = null!)
        {
            await signInManager.SignOutAsync().ConfigureAwait(false);
            logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
