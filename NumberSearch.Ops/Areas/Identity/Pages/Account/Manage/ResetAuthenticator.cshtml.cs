using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class ResetAuthenticatorModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<ResetAuthenticatorModel> logger) : PageModel
    {
        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
            await userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            logger.LogInformation("User with ID '{UserId}' has reset their authentication app key.", user.Id);

            await signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";

            return RedirectToPage("./EnableAuthenticator");
        }
    }
}