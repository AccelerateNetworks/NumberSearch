using Flurl.Http;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Peerless
{

    public class DidFind
    {
        public string did { get; set; }
        public string category { get; set; }

        public static async Task<IEnumerable<DidFind>> GetRawAsync(string npa, string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/find";
            string npaParameter = $"?npa={npa}";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{apiKeyParameter}";
            return await route.GetJsonAsync<IEnumerable<DidFind>>().ConfigureAwait(false);
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string query, string apiKey)
        {
            var results = await GetRawAsync(query, apiKey).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || results.Count() == 0)
            {
                return list;
            }

            foreach (var item in results)
            {
                bool checkNpa = int.TryParse(item.did.Substring(0, 3), out int npa);
                bool checkNxx = int.TryParse(item.did.Substring(3, 3), out int nxx);
                bool checkXxxx = int.TryParse(item.did.Substring(6), out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    list.Add(new PhoneNumber
                    {
                        NPA = npa,
                        NXX = nxx,
                        XXXX = xxxx,
                        DialedNumber = item.did,
                        City = "Unknown City",
                        State = "Unknown State",
                        IngestedFrom = "Peerless"
                    });
                }
            }
            return list.ToArray();
        }
    }
}