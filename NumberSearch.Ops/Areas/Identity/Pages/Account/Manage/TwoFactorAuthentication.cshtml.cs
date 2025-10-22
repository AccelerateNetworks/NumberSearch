using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class TwoFactorAuthenticationModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager) : PageModel
    {
        public bool HasAuthenticator { get; set; }

        public int RecoveryCodesLeft { get; set; }

        [BindProperty]
        public bool Is2faEnabled { get; set; }

        public bool IsMachineRemembered { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            HasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false) != null;
            Is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
            IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user).ConfigureAwait(false);
            RecoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user).ConfigureAwait(false);

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await signInManager.ForgetTwoFactorClientAsync().ConfigureAwait(false);
            StatusMessage = "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.";
            return RedirectToPage();
        }
    }
}