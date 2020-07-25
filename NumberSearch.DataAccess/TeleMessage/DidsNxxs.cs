using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleMesssage
{
    public class DidsNxxs
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Mirrors the remote API")]
        public string[] data { get; set; }

        public static async Task<DidsNxxs> GetAsync(string npa, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/nxxs";
            string tokenParameter = $"?token={token}";
            string availableParameter = $"&available=true";
            string npaParameter = $"&npa={npa}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{availableParameter}{npaParameter}";
            return await route.GetJsonAsync<DidsNxxs>().ConfigureAwait(false);
        }
    }
}
