using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager) : PageModel
    {
        public string Username { get; set; } = null!;

        [TempData]
        public string StatusMessage { get; set; } = null!;

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; } = null!;
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await userManager.GetUserNameAsync(user).ConfigureAwait(false);
            var phoneNumber = await userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);

            Username = userName ?? string.Empty;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber ?? string.Empty
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user).ConfigureAwait(false);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user).ConfigureAwait(false);
                return Page();
            }

            var phoneNumber = await userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber).ConfigureAwait(false);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await signInManager.RefreshSignInAsync(user).ConfigureAwait(false);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
