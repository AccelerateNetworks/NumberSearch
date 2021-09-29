using Flurl.Http;

using Serilog;

using System.Threading.Tasks;

using static NumberSearch.DataAccess.Call48.Search;

namespace NumberSearch.DataAccess.Call48
{
    public class Purchase
    {
        /// <summary>
        /// Get a valid security token from Call48.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<PurchaseResult> PurchasePhoneNumberAsync(string loc, SearchResult number, string token)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"purchase";

            string route = $"{baseUrl}{endPointName}";

            // Get the request body into the correct format.
            var purchase = new PurchaseNumber
            {
                npa = number.npa,
                nxx = number.nxx,
                xxxx = number.xxxx,
                type = "local",
                state = number.state,
                ratecenter = number.ratecenter,
                locData = number.locData,
                loc = loc,
                fwd_trunk_grpid = 78,
                fwd_preconfigured = true
            };

            var request = new PurchaseRequest
            {
                type = "local",
                numbers = new PurchaseNumber[]
                {
                    purchase
                }
            };

            try
            {
                var result = await route.WithHeader("Authorization", token).PostJsonAsync(request).ReceiveJson<PurchaseResult>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseJsonAsync<PurchaseResult>();
                Log.Error($"[Ingest] [Call48] Failed to purchase {number.did}.");
                return error;
            }
        }

        public class PurchaseRequest
        {
            public string type { get; set; }
            public PurchaseNumber[] numbers { get; set; }
        }

        public class PurchaseResult
        {
            public int code { get; set; }
            public string message { get; set; }
            public bool data { get; set; }
            public string error { get; set; }
        }

        public class PurchaseNumber
        {
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public string type { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string locData { get; set; }
            public string loc { get; set; }
            public int fwd_trunk_grpid { get; set; }
            public bool fwd_preconfigured { get; set; }
        }
    }
}
