using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Peerless
{

    public class Lerg
    {
        public Lata[] lata { get; set; } = Array.Empty<Lata>();
        public string[] location { get; set; } = Array.Empty<string>();
        public string[] rate_center { get; set; } = Array.Empty<string>();
        public string[] state { get; set; } = Array.Empty<string>();
        public int[] npa { get; set; } = Array.Empty<int>();
        public int[] nxx { get; set; } = Array.Empty<int>();

        public static async Task<Lerg> GetAsync(int npa, string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "lerg";
            string npaParameter = $"?npa={npa:000}";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{apiKeyParameter}";
            return await route.GetJsonAsync<Lerg>().ConfigureAwait(false);
        }

        public class Lata
        {
            public string id { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
        }
    }
}