using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Areas.Identity.Pages.Account.Manage
{
    public class DownloadPersonalDataModel(
        UserManager<IdentityUser> userManager,
        ILogger<DownloadPersonalDataModel> logger) : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            logger.LogInformation("User with ID '{UserId}' asked for their personal data.", userManager.GetUserId(User));

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(IdentityUser).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await userManager.GetLoginsAsync(user).ConfigureAwait(false);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json");
        }
    }
}
