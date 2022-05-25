using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Peerless
{

    public class Lerg
    {
        public Lata[] lata { get; set; }
        public string[] location { get; set; }
        public string[] rate_center { get; set; }
        public string[] state { get; set; }
        public int[] npa { get; set; }
        public int[] nxx { get; set; }

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
            public string id { get; set; }
            public string name { get; set; }
        }
    }
}