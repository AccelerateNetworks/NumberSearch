using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMesssage
{

    public class DidsNpas
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public string[] data { get; set; }

        public static async Task<DidsNpas> GetAsync(Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/npas";
            string tokenParameter = $"?token={token}";
            string availableParameter = $"&available=true";
            //string typeParameter = $"&type=true";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{availableParameter}";
            return await route.GetJsonAsync<DidsNpas>().ConfigureAwait(false);
        }
    }
}
