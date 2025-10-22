using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailChangeModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : PageModel
    {
        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await userManager.ChangeEmailAsync(user, email, code).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                StatusMessage = "Error changing email.";
                return Page();
            }

            // In our UI email and user name are one and the same, so when we update the email
            // we need to update the user name.
            var setUserNameResult = await userManager.SetUserNameAsync(user, email).ConfigureAwait(false);
            if (!setUserNameResult.Succeeded)
            {
                StatusMessage = "Error changing user name.";
                return Page();
            }

            await signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            StatusMessage = "Thank you for confirming your email change.";
            return Page();
        }
    }
}
