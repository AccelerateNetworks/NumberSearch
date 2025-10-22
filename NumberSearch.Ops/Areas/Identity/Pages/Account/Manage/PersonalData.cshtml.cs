using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class PersonalDataModel(
        UserManager<IdentityUser> userManager,
        ILogger<PersonalDataModel> logger) : PageModel
    {
        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            return Page();
        }
    }
}