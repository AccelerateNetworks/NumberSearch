using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct CnamBulkVs(string name, string number, long time, DateTime LastChecked)
    {
        /// <summary>
        /// Get CNAM lookup information for a specific dialed number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<CnamBulkVs> GetAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> apiKey)
        {
            string baseUrl = "https://cnam.bulkvs.com/";
            string apikeyParameter = $"?id={apiKey}";
            string didParameter = $"&did={dialedNumber}";
            string formatParameter = $"&format=json";
            string route = $"{baseUrl}{apikeyParameter}{didParameter}{formatParameter}";

            try
            {
                var result = await route.GetJsonAsync<CnamBulkVs>();
                // Handle the time requested.
                // https://stackoverflow.com/questions/2477712/convert-local-time-10-digit-number-to-a-readable-datetime-format
                result = result with { LastChecked = new DateTime(1970, 1, 1).AddSeconds(result.time) };
                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Error($"Failed to parse response from BulkVS for {dialedNumber}");
                Log.Error(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }
}
