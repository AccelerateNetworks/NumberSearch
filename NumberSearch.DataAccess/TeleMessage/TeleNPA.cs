using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{

    public class TeleNPA
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Pending>")]
        public string[] data { get; set; }

        public static async Task<TeleNPA> GetAsync(Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/npas";
            string tokenParameter = $"?token={token}";
            string availableParameter = $"&available=true";
            //string typeParameter = $"&type=true";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{availableParameter}";
            return await route.GetJsonAsync<TeleNPA>();
        }
    }
}
