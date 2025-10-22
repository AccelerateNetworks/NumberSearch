using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class ExternalLoginsModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager) : PageModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; } = null!;

        public IList<AuthenticationScheme> OtherLogins { get; set; } = null!;

        public bool ShowRemoveButton { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            CurrentLogins = await userManager.GetLoginsAsync(user).ConfigureAwait(false);
            OtherLogins = [.. (await signInManager.GetExternalAuthenticationSchemesAsync().ConfigureAwait(false)).Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))];
            ShowRemoveButton = user.PasswordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not removed.";
                return RedirectToPage();
            }

            await signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            StatusMessage = "The external login was removed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var info = await signInManager.GetExternalLoginInfoAsync(user.Id).ConfigureAwait(false) ?? throw new InvalidOperationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            var result = await userManager.AddLoginAsync(user, info).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not added. External logins can only be associated with one account.";
                return RedirectToPage();
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);

            StatusMessage = "The external login was added.";
            return RedirectToPage();
        }
    }
}
