﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class ResetAuthenticatorModel : PageModel
    {
        readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        readonly ILogger<ResetAuthenticatorModel> _logger;

        public ResetAuthenticatorModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<ResetAuthenticatorModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; } = null!;

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
            await _userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            _logger.LogInformation("User with ID '{UserId}' has reset their authentication app key.", user.Id);

            await _signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            StatusMessage = "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.";

            return RedirectToPage("./EnableAuthenticator");
        }
    }
}