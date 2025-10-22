using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class Disable2faModel(
        UserManager<IdentityUser> userManager,
        ILogger<Disable2faModel> logger) : PageModel
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

            if (!await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Cannot disable 2FA for user with ID '{userManager.GetUserId(User)}' as it's not currently enabled.");
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

            var disable2faResult = await userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
            if (!disable2faResult.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred disabling 2FA for user with ID '{userManager.GetUserId(User)}'.");
            }

            logger.LogInformation("User with ID '{UserId}' has disabled 2fa.", userManager.GetUserId(User));
            StatusMessage = "2fa has been disabled. You can reenable 2fa when you setup an authenticator app";
            return RedirectToPage("./TwoFactorAuthentication");
        }
    }
}