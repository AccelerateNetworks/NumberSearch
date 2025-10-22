using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginWithRecoveryCodeModel(SignInManager<IdentityUser> signInManager, ILogger<LoginWithRecoveryCodeModel> logger) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public string ReturnUrl { get; set; } = null!;

        public class InputModel
        {
            [BindProperty]
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Recovery Code")]
            public string RecoveryCode { get; set; } = null!;
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null!)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false) ?? throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null!)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false) ?? throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode).ConfigureAwait(false);

            if (result.Succeeded)
            {
                logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", user.Id);
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }
            if (result.IsLockedOut)
            {
                logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
                return RedirectToPage("./Lockout");
            }
            else
            {
                logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}' ", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return Page();
            }
        }
    }
}
