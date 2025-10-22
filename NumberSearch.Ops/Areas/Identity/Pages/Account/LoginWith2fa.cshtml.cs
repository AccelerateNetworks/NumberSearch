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
    public class LoginWith2faModel(SignInManager<IdentityUser> signInManager, ILogger<LoginWith2faModel> logger) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Authenticator code")]
            public string? TwoFactorCode { get; set; }

            [Display(Name = "Remember this machine")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null!)
        {
            // Ensure the user has gone through the username & password screen first
            _ = await signInManager.GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false) ?? throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null!)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl ??= Url.Content("~/");

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync().ConfigureAwait(false) ?? throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            var authenticatorCode = Input.TwoFactorCode?.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode ?? string.Empty, rememberMe, Input.RememberMachine).ConfigureAwait(false);

            if (result.Succeeded)
            {
                logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
                return LocalRedirect(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
                return RedirectToPage("./Lockout");
            }
            else
            {
                logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return Page();
            }
        }
    }
}
