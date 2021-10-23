using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

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
        public static async Task<PurchaseResult> PurchasePhoneNumberAsync(PhoneNumber number, string token)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"purchase";

            string route = $"{baseUrl}{endPointName}";

            // Get the request body into the correct format.
            var purchase = new PurchaseNumber
            {
                npa = number.NPA.ToString("000"),
                nxx = number.NXX.ToString("000"),
                xxxx = number.XXXX.ToString("0000")
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
                return await route.WithHeader("Authorization", token).PostJsonAsync(request).ReceiveJson<PurchaseResult>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
                Log.Fatal($"[Ingest] [Call48] Failed to purchase {number.DialedNumber}.");
                Log.Fatal(error);
                return null;
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
            public PurchaseData data { get; set; }
            public string error { get; set; }
        }

        public class PurchaseData
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int fulfilled_quantity { get; set; }
        }

        public class PurchaseNumber
        {
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
        }
    }
}
