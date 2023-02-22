using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class DidsNpas
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public string[] data { get; set; } = Array.Empty<string>();

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
