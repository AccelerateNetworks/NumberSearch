using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class LocalNumberPortability
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Mirrors the external API")]
        public dynamic data { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Mirrors the external API")]
        public class LocalNumberPortabilityResult
        {
            public string status { get; set; }
            public string message { get; set; }
        }

        public static async Task<string> GetRawAsync(string query, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "lnp/check";
            string tokenParameter = $"?token={token}";
            string searchParameter = $"&numbers={query}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{searchParameter}";
            return await url.GetStringAsync().ConfigureAwait(false);
        }

        public static async Task<bool> IsPortable(string query, Guid token)
        {
            var results = await GetRawAsync(query, token).ConfigureAwait(false);


            if (results.Contains("error"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}